
using Dim.Abstractions.Routing;
using Dim.AspNetCore.Authentication;

namespace Dim.SampleHost.Authentication;

public class TokenVerify : IDimTokenValidator
{
    public Task<DimChatAuthenticationResult?> ValidateAsync(string token, CancellationToken cancellationToken)
    {
        return Task.FromResult<DimChatAuthenticationResult?>(new DimChatAuthenticationResult("1000", DimClientPlatform.Android));
    }
}
