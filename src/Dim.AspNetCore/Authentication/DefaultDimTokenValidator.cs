namespace Dim.AspNetCore.Authentication;

internal sealed class DefaultDimTokenValidator : IDimTokenValidator
{
    public Task<DimChatAuthenticationResult?> ValidateAsync(
        string token,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<DimChatAuthenticationResult?>(null);
    }
}
