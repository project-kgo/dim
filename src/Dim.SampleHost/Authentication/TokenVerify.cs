using Dim.Abstractions.Authentication;
using Dim.Abstractions.Routing;

namespace Dim.SampleHost.Authentication;

public class TokenVerify : ITokenValidator
{
    public Task<AuthenticationResult?> ValidateAsync(string token, CancellationToken cancellationToken)
    {
        return Task.FromResult<AuthenticationResult?>(new AuthenticationResult("1000", DimClientPlatform.Android));
    }
}
