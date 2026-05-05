using Dim.Abstractions.Authentication;
using Dim.Application.Authentication;
using Dim.Infrastructure.Authentication;
using FluentAssertions;
using StackExchange.Redis;

namespace Dim.UnitTests.Authentication;

public sealed class RedisTokenValidatorStoreTests
{
    [Fact]
    public void ParseAccessTokenInfoShouldReturnTokenInfoWhenHashIsValid()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeMilliseconds();
        var ttl = TimeSpan.FromDays(7);

        var tokenInfo = RedisTokenValidatorStore.ParseAccessTokenInfo(
            [
                new HashEntry("loginId", "u1:Web"),
                new HashEntry("accessToken", "token"),
                new HashEntry("expiresAt", expiresAt),
                new HashEntry("ttl", (long)ttl.TotalMilliseconds)
            ]);

        tokenInfo.Should().Be(new AccessTokenInfo(
            new LoginId("u1:Web"),
            "token",
            DateTimeOffset.FromUnixTimeMilliseconds(expiresAt),
            ttl));
    }

    [Fact]
    public void ParseAccessTokenInfoShouldReturnNullWhenRequiredFieldIsMissing()
    {
        var tokenInfo = RedisTokenValidatorStore.ParseAccessTokenInfo(
            [
                new HashEntry("loginId", "u1:Web"),
                new HashEntry("expiresAt", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
                new HashEntry("ttl", (long)TimeSpan.FromDays(7).TotalMilliseconds)
            ]);

        tokenInfo.Should().BeNull();
    }

    [Fact]
    public void ParseAccessTokenInfoShouldReturnNullWhenExpiresAtIsInvalid()
    {
        var tokenInfo = RedisTokenValidatorStore.ParseAccessTokenInfo(
            [
                new HashEntry("loginId", "u1:Web"),
                new HashEntry("accessToken", "token"),
                new HashEntry("expiresAt", "bad"),
                new HashEntry("ttl", (long)TimeSpan.FromDays(7).TotalMilliseconds)
            ]);

        tokenInfo.Should().BeNull();
    }

    [Fact]
    public void ParseAccessTokenInfoShouldReturnNullWhenTtlIsInvalid()
    {
        var tokenInfo = RedisTokenValidatorStore.ParseAccessTokenInfo(
            [
                new HashEntry("loginId", "u1:Web"),
                new HashEntry("accessToken", "token"),
                new HashEntry("expiresAt", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
                new HashEntry("ttl", "bad")
            ]);

        tokenInfo.Should().BeNull();
    }
}
