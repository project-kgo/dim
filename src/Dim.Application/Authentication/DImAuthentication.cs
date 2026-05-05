using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Dim.Abstractions.Authentication;
using Dim.Abstractions.Routing;

namespace Dim.Application.Authentication;

public sealed class DImAuthentication(ITokenValidatorStore tokenValidatorStore) : IAuthentication
{
    private const char TokenSeparator = '.';
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromDays(15);
    private static readonly TimeSpan RefreshAhead = TimeSpan.FromDays(1);

    private readonly ITokenValidatorStore _tokenValidatorStore = tokenValidatorStore;

    public async Task<AccessTokenInfo> GenerateAccessTokenAsync(
        LoginId loginId,
        TimeSpan ttl,
        CancellationToken ct = default)
    {
        var normalizedTtl = NormalizeTtl(ttl);
        var expiresAt = DateTimeOffset.UtcNow.Add(normalizedTtl);
        var accessTokenInfo = new AccessTokenInfo(
            loginId,
            GenerateAccessToken(loginId),
            expiresAt,
            normalizedTtl);

        await _tokenValidatorStore.SetAccessToken(accessTokenInfo, ct);

        return accessTokenInfo;
    }

    public async Task<bool> ValidateAccessTokenAsync(
        LoginId loginId,
        string accessToken,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        var accessTokenInfo = await _tokenValidatorStore.GetAccessToken(loginId.Value, ct);
        if (accessTokenInfo is null || IsExpired(accessTokenInfo.ExpiresAt, now))
        {
            return false;
        }

        if (!FixedTimeEquals(accessTokenInfo.AccessToken, accessToken))
        {
            return false;
        }

        await RefreshIfNeededAsync(accessTokenInfo, now, ct);

        return true;
    }

    public Task RefreshAccessTokenAsync(
        LoginId loginId,
        DateTimeOffset expiresAt,
        TimeSpan ttl,
        CancellationToken ct = default)
    {
        var normalizedTtl = NormalizeTtl(ttl);
        return _tokenValidatorStore.RefreshAccessToken(
            loginId.Value,
            expiresAt.ToUniversalTime(),
            normalizedTtl,
            ct);
    }

    public Task RemoveAccessTokenAsync(LoginId loginId, CancellationToken ct = default)
    {
        return _tokenValidatorStore.RemoveAccessToken(loginId.Value, ct);
    }

    public async Task<AuthenticationResult?> ValidateAsync(
        string token,
        CancellationToken cancellationToken)
    {
        if (!TryGetLoginId(token, out var loginId))
        {
            return null;
        }

        if (!await ValidateAccessTokenAsync(loginId, token, cancellationToken))
        {
            return null;
        }

        return TryCreateAuthenticationResult(loginId, out var result)
            ? result
            : null;
    }

    private static string GenerateAccessToken(LoginId loginId)
    {
        var loginIdBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(loginId.Value));
        var timestampNanoseconds = GetUnixTimestampNanoseconds(DateTimeOffset.UtcNow);
        var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(
            timestampNanoseconds.ToString(CultureInfo.InvariantCulture)));
        var key = Convert.ToHexString(keyBytes).ToLowerInvariant();

        return $"{loginIdBase64}{TokenSeparator}{key}";
    }

    private static bool TryGetLoginId(string? token, out LoginId loginId)
    {
        loginId = default;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var separatorIndex = token.IndexOf(TokenSeparator, StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            return false;
        }

        try
        {
            var loginIdBytes = Convert.FromBase64String(token[..separatorIndex]);
            var loginIdValue = Encoding.UTF8.GetString(loginIdBytes);
            if (string.IsNullOrWhiteSpace(loginIdValue))
            {
                return false;
            }

            loginId = new LoginId(loginIdValue);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryCreateAuthenticationResult(
        LoginId loginId,
        out AuthenticationResult? result)
    {
        result = null;

        var loginIdValue = loginId.Value;
        var firstSeparatorIndex = loginIdValue.IndexOf(':', StringComparison.Ordinal);
        var lastSeparatorIndex = loginIdValue.LastIndexOf(':');
        if (firstSeparatorIndex <= 0
            || lastSeparatorIndex <= firstSeparatorIndex
            || lastSeparatorIndex == loginIdValue.Length - 1)
        {
            return false;
        }

        if (!long.TryParse(
                loginIdValue[..firstSeparatorIndex],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var appId))
        {
            return false;
        }

        var userId = loginIdValue[(firstSeparatorIndex + 1)..lastSeparatorIndex];
        var platformValue = loginIdValue[(lastSeparatorIndex + 1)..];
        if (appId <= 0
            || string.IsNullOrWhiteSpace(userId)
            || !DimClientPlatformParser.TryParse(platformValue, out var platform))
        {
            return false;
        }

        result = new AuthenticationResult(appId, userId, platform);
        return true;
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private async Task RefreshIfNeededAsync(
        AccessTokenInfo accessTokenInfo,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var ttl = NormalizeTtl(accessTokenInfo.Ttl);
        var refreshThreshold = ttl - RefreshAhead;
        if (refreshThreshold <= TimeSpan.Zero)
        {
            return;
        }

        var remaining = accessTokenInfo.ExpiresAt.ToUniversalTime() - now;
        if (remaining >= refreshThreshold)
        {
            return;
        }

        var newExpiresAt = now.Add(ttl);
        await _tokenValidatorStore.RefreshAccessToken(
            accessTokenInfo.LoginId.Value,
            newExpiresAt,
            ttl,
            ct);
    }

    private static bool IsExpired(DateTimeOffset expiresAt, DateTimeOffset now)
    {
        return expiresAt.ToUniversalTime() <= now;
    }

    private static TimeSpan NormalizeTtl(TimeSpan ttl)
    {
        return ttl > TimeSpan.Zero ? ttl : DefaultTtl;
    }

    private static long GetUnixTimestampNanoseconds(DateTimeOffset timestamp)
    {
        return checked((timestamp.UtcTicks - DateTimeOffset.UnixEpoch.UtcTicks) * 100L);
    }

    public async Task<string?> AuthenticateAsync(LoginId loginId, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var result = await GenerateAccessTokenAsync(loginId, ttl, cancellationToken);
        return result.AccessToken;
    }
}
