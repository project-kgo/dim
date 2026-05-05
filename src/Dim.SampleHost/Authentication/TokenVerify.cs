using System.Globalization;
using Dim.Abstractions.Authentication;
using Dim.Abstractions.Routing;

namespace Dim.SampleHost.Authentication;

public class TokenVerify : ITokenValidator
{
    private const long AppId = 1001;
    private long userId = 1000;
    public Task<AuthenticationResult?> ValidateAsync(string token, CancellationToken cancellationToken)
    {
        userId++;
        return Task.FromResult<AuthenticationResult?>(new AuthenticationResult(
            AppId,
            userId.ToString(CultureInfo.InvariantCulture),
            DimClientPlatform.Android));
    }
}
