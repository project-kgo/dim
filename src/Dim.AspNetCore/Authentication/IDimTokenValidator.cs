namespace Dim.AspNetCore.Authentication;

public interface IDimTokenValidator
{
    Task<DimChatAuthenticationResult?> ValidateAsync(
        string token,
        CancellationToken cancellationToken);
}
