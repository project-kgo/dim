using System.Globalization;
using Dim.Abstractions.Authentication;
using Dim.Application.Authentication;
using StackExchange.Redis;

namespace Dim.Infrastructure.Authentication;

public sealed class RedisTokenValidatorStore(IConnectionMultiplexer connectionMultiplexer) : ITokenValidatorStore
{
    private const string KeyPrefix = "dim:tokens";
    private const string LoginIdField = "loginId";
    private const string AccessTokenField = "accessToken";
    private const string ExpiresAtField = "expiresAt";
    private const string TtlField = "ttl";

    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

    public async Task SetAccessToken(AccessTokenInfo accessTokenInfo, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var expiresAt = accessTokenInfo.ExpiresAt.ToUniversalTime();
        var ttl = accessTokenInfo.Ttl;
        var redisTtl = expiresAt - DateTimeOffset.UtcNow;
        var key = TokenKey(accessTokenInfo.LoginId.Value);
        if (ttl <= TimeSpan.Zero || redisTtl <= TimeSpan.Zero)
        {
            await _database.KeyDeleteAsync(key);
            return;
        }

        await _database.HashSetAsync(
            key,
            [
                new HashEntry(LoginIdField, accessTokenInfo.LoginId.Value),
                new HashEntry(AccessTokenField, accessTokenInfo.AccessToken),
                new HashEntry(ExpiresAtField, ToUnixTimeMilliseconds(expiresAt)),
                new HashEntry(TtlField, ToMilliseconds(ttl))
            ]);
        await _database.KeyExpireAsync(key, redisTtl);
    }

    public async Task<AccessTokenInfo?> GetAccessToken(string loginId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(loginId))
        {
            return null;
        }

        var key = TokenKey(loginId);
        var entries = await _database.HashGetAllAsync(key);
        var accessTokenInfo = ParseAccessTokenInfo(entries);
        if (accessTokenInfo is null || accessTokenInfo.LoginId.Value != loginId)
        {
            return null;
        }

        if (accessTokenInfo.ExpiresAt.ToUniversalTime() > DateTimeOffset.UtcNow)
        {
            return accessTokenInfo;
        }

        await _database.KeyDeleteAsync(key);
        return null;
    }

    public async Task RefreshAccessToken(
        string loginId,
        DateTimeOffset expiresAt,
        TimeSpan ttl,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(loginId))
        {
            return;
        }

        var utcExpiresAt = expiresAt.ToUniversalTime();
        var redisTtl = utcExpiresAt - DateTimeOffset.UtcNow;
        var key = TokenKey(loginId);
        if (ttl <= TimeSpan.Zero || redisTtl <= TimeSpan.Zero)
        {
            await _database.KeyDeleteAsync(key);
            return;
        }

        if (!await _database.KeyExistsAsync(key))
        {
            return;
        }

        await _database.HashSetAsync(
            key,
            [
                new HashEntry(ExpiresAtField, ToUnixTimeMilliseconds(utcExpiresAt)),
                new HashEntry(TtlField, ToMilliseconds(ttl))
            ]);
        await _database.KeyExpireAsync(key, redisTtl);
    }

    public Task RemoveAccessToken(string loginId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return string.IsNullOrWhiteSpace(loginId)
            ? Task.CompletedTask
            : _database.KeyDeleteAsync(TokenKey(loginId));
    }

    internal static AccessTokenInfo? ParseAccessTokenInfo(HashEntry[] entries)
    {
        if (entries.Length == 0)
        {
            return null;
        }

        var values = entries.ToDictionary(
            entry => entry.Name.ToString(),
            entry => entry.Value.ToString(),
            StringComparer.Ordinal);

        if (!values.TryGetValue(LoginIdField, out var loginId)
            || string.IsNullOrWhiteSpace(loginId)
            || !values.TryGetValue(AccessTokenField, out var accessToken)
            || string.IsNullOrWhiteSpace(accessToken)
            || !values.TryGetValue(ExpiresAtField, out var expiresAtValue)
            || !long.TryParse(expiresAtValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var expiresAtMilliseconds)
            || !values.TryGetValue(TtlField, out var ttlValue)
            || !long.TryParse(ttlValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ttlMilliseconds)
            || ttlMilliseconds <= 0)
        {
            return null;
        }

        var expiresAt = DateTimeOffset
            .FromUnixTimeMilliseconds(expiresAtMilliseconds);

        return new AccessTokenInfo(
            new LoginId(loginId),
            accessToken,
            expiresAt,
            TimeSpan.FromMilliseconds(ttlMilliseconds));
    }

    private static string TokenKey(string loginId)
    {
        return $"{KeyPrefix}:{{{loginId}}}";
    }

    private static long ToUnixTimeMilliseconds(DateTimeOffset expiresAt)
    {
        return expiresAt.ToUniversalTime().ToUnixTimeMilliseconds();
    }

    private static long ToMilliseconds(TimeSpan ttl)
    {
        return checked(ttl.Ticks / TimeSpan.TicksPerMillisecond);
    }
}
