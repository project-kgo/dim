namespace Dim.AspNetCore.Authentication;

internal sealed class DefaultDimChatAuthenticator : IDimAuthenticator
{
    public ValueTask<DimChatAuthenticationResult?> AuthenticateAsync(
        DimAuthenticationContext context,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult<DimChatAuthenticationResult?>(null);
    }
}
