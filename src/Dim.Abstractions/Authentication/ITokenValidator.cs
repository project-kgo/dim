namespace Dim.Abstractions.Authentication;

public interface ITokenValidator
{
    Task<AuthenticationResult?> ValidateAsync(
        string token,
        CancellationToken cancellationToken);
}
