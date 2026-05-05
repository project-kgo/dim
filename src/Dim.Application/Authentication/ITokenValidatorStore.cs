

namespace Dim.Application.Authentication;

public interface ITokenValidatorStore
{
    public Task SetAccessToken(AccessTokenInfo accessTokenInfo, CancellationToken ct = default);

    public Task<AccessTokenInfo?> GetAccessToken(string loginId, CancellationToken ct = default);

    public Task RefreshAccessToken(string loginId, DateTimeOffset expiresAt, TimeSpan ttl, CancellationToken ct = default);

    public Task RemoveAccessToken(string loginId, CancellationToken ct = default);

}
