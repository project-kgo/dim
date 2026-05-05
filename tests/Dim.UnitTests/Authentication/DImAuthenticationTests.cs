using System.Text;
using System.Text.RegularExpressions;
using Dim.Abstractions.Authentication;
using Dim.Abstractions.Routing;
using Dim.Application.Authentication;
using FluentAssertions;

namespace Dim.UnitTests.Authentication;

public sealed class DImAuthenticationTests
{
    private const long AppId = 1001;

    [Fact]
    public async Task GenerateAccessTokenAsyncShouldStoreTokenWithLoginIdPrefix()
    {
        var store = new TestTokenValidatorStore();
        var authentication = new DImAuthentication(store);
        var loginId = new LoginId(AppId, "u1", DimClientPlatform.Web);
        var ttl = TimeSpan.FromMinutes(30);
        var before = DateTimeOffset.UtcNow;

        var tokenInfo = await authentication.GenerateAccessTokenAsync(loginId, ttl);

        tokenInfo.LoginId.Should().Be(loginId);
        tokenInfo.ExpiresAt.Should().BeAfter(before);
        tokenInfo.Ttl.Should().Be(ttl);
        store.AccessTokens[loginId.Value].Should().Be(tokenInfo);

        var tokenParts = tokenInfo.AccessToken.Split('.');
        tokenParts.Should().HaveCount(2);
        Encoding.UTF8.GetString(Convert.FromBase64String(tokenParts[0]))
            .Should()
            .Be(loginId.Value);
        Regex.IsMatch(tokenParts[1], "^[0-9a-f]{64}$").Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAccessTokenAsyncShouldReturnTrueWhenTokenMatchesAndNotExpired()
    {
        var store = new TestTokenValidatorStore();
        var authentication = new DImAuthentication(store);
        var loginId = new LoginId(AppId, "u1", DimClientPlatform.Android);
        var tokenInfo = await authentication.GenerateAccessTokenAsync(loginId, TimeSpan.FromMinutes(30));

        var isValid = await authentication.ValidateAccessTokenAsync(loginId, tokenInfo.AccessToken);

        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAccessTokenAsyncShouldReturnFalseWhenTokenIsInvalid()
    {
        var store = new TestTokenValidatorStore();
        var authentication = new DImAuthentication(store);
        var loginId = new LoginId(AppId, "u1", DimClientPlatform.Ios);
        await authentication.GenerateAccessTokenAsync(loginId, TimeSpan.FromMinutes(30));

        var isValid = await authentication.ValidateAccessTokenAsync(loginId, "bad-token");

        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAccessTokenAsyncShouldReturnFalseWhenTokenIsExpired()
    {
        var store = new TestTokenValidatorStore();
        var authentication = new DImAuthentication(store);
        var loginId = new LoginId(AppId, "u1", DimClientPlatform.Web);
        store.AccessTokens[loginId.Value] = new AccessTokenInfo(
            loginId,
            "token",
            DateTimeOffset.UtcNow.AddMinutes(-1),
            TimeSpan.FromDays(7));

        var isValid = await authentication.ValidateAccessTokenAsync(loginId, "token");

        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsyncShouldReturnAuthenticationResultWhenTokenIsValid()
    {
        var store = new TestTokenValidatorStore();
        var authentication = new DImAuthentication(store);
        var loginId = new LoginId(AppId, "u1", DimClientPlatform.Android);
        var tokenInfo = await authentication.GenerateAccessTokenAsync(loginId, TimeSpan.FromMinutes(30));

        var result = await authentication.ValidateAsync(tokenInfo.AccessToken, CancellationToken.None);

        result.Should().Be(new AuthenticationResult(AppId, "u1", DimClientPlatform.Android));
    }

    [Fact]
    public async Task RefreshAndRemoveAccessTokenAsyncShouldDelegateToStore()
    {
        var store = new TestTokenValidatorStore();
        var authentication = new DImAuthentication(store);
        var loginId = new LoginId(AppId, "u1", DimClientPlatform.Web);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        var ttl = TimeSpan.FromHours(1);

        await authentication.RefreshAccessTokenAsync(loginId, expiresAt, ttl);
        await authentication.RemoveAccessTokenAsync(loginId);

        store.RefreshedLoginId.Should().Be(loginId.Value);
        store.RefreshedExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));
        store.RefreshedTtl.Should().Be(ttl);
        store.RemovedLoginId.Should().Be(loginId.Value);
    }

    [Fact]
    public async Task ValidateAccessTokenAsyncShouldRefreshWhenRemainingLessThanTtlMinusOneDay()
    {
        var store = new TestTokenValidatorStore();
        var authentication = new DImAuthentication(store);
        var loginId = new LoginId(AppId, "u1", DimClientPlatform.Web);
        var ttl = TimeSpan.FromDays(7);
        store.AccessTokens[loginId.Value] = new AccessTokenInfo(
            loginId,
            "token",
            DateTimeOffset.UtcNow.AddDays(5),
            ttl);

        var isValid = await authentication.ValidateAccessTokenAsync(loginId, "token");

        isValid.Should().BeTrue();
        store.RefreshedLoginId.Should().Be(loginId.Value);
        store.RefreshedExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow.AddDays(6));
        store.RefreshedTtl.Should().Be(ttl);
    }

    [Fact]
    public async Task ValidateAccessTokenAsyncShouldNotRefreshWhenRemainingIsEnough()
    {
        var store = new TestTokenValidatorStore();
        var authentication = new DImAuthentication(store);
        var loginId = new LoginId(AppId, "u1", DimClientPlatform.Web);
        store.AccessTokens[loginId.Value] = new AccessTokenInfo(
            loginId,
            "token",
            DateTimeOffset.UtcNow.AddDays(6).AddMinutes(5),
            TimeSpan.FromDays(7));

        var isValid = await authentication.ValidateAccessTokenAsync(loginId, "token");

        isValid.Should().BeTrue();
        store.RefreshedLoginId.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAccessTokenAsyncShouldNotRefreshWhenTtlIsLessThanOrEqualOneDay()
    {
        var store = new TestTokenValidatorStore();
        var authentication = new DImAuthentication(store);
        var loginId = new LoginId(AppId, "u1", DimClientPlatform.Web);
        store.AccessTokens[loginId.Value] = new AccessTokenInfo(
            loginId,
            "token",
            DateTimeOffset.UtcNow.AddHours(12),
            TimeSpan.FromDays(1));

        var isValid = await authentication.ValidateAccessTokenAsync(loginId, "token");

        isValid.Should().BeTrue();
        store.RefreshedLoginId.Should().BeNull();
    }

    private sealed class TestTokenValidatorStore : ITokenValidatorStore
    {
        public Dictionary<string, AccessTokenInfo> AccessTokens { get; } = new(StringComparer.Ordinal);

        public string? RefreshedLoginId { get; private set; }

        public DateTimeOffset? RefreshedExpiresAt { get; private set; }

        public TimeSpan? RefreshedTtl { get; private set; }

        public string? RemovedLoginId { get; private set; }

        public Task SetAccessToken(AccessTokenInfo accessTokenInfo, CancellationToken ct = default)
        {
            AccessTokens[accessTokenInfo.LoginId.Value] = accessTokenInfo;
            return Task.CompletedTask;
        }

        public Task<AccessTokenInfo?> GetAccessToken(string loginId, CancellationToken ct = default)
        {
            AccessTokens.TryGetValue(loginId, out var accessTokenInfo);
            return Task.FromResult(accessTokenInfo);
        }

        public Task RefreshAccessToken(
            string loginId,
            DateTimeOffset expiresAt,
            TimeSpan ttl,
            CancellationToken ct = default)
        {
            RefreshedLoginId = loginId;
            RefreshedExpiresAt = expiresAt;
            RefreshedTtl = ttl;
            return Task.CompletedTask;
        }

        public Task RemoveAccessToken(string loginId, CancellationToken ct = default)
        {
            RemovedLoginId = loginId;
            return Task.CompletedTask;
        }
    }
}
