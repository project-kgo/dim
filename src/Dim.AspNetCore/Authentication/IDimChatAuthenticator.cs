namespace Dim.AspNetCore.Authentication;

public interface IDimAuthenticator
{
    ValueTask<DimChatAuthenticationResult?> AuthenticateAsync(
        DimAuthenticationContext context,
        CancellationToken cancellationToken);
}
