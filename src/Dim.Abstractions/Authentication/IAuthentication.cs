
namespace Dim.Abstractions.Authentication;

public interface IAuthentication: ITokenValidator
{
    Task<string?> AuthenticateAsync(LoginId loginId, TimeSpan ttl, CancellationToken cancellationToken);
}
