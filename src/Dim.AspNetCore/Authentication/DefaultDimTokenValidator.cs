using Dim.Abstractions.Authentication;

namespace Dim.AspNetCore.Authentication;

internal sealed class DefaultDimTokenValidator : ITokenValidator
{
    public Task<AuthenticationResult?> ValidateAsync(
        string token,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<AuthenticationResult?>(null);
    }
}
