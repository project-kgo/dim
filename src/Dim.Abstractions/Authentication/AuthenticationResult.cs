using Dim.Abstractions.Routing;

namespace Dim.Abstractions.Authentication;

public sealed record AuthenticationResult(
    long AppId,
    string UserId,
    DimClientPlatform Platform);
