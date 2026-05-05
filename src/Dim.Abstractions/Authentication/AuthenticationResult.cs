using Dim.Abstractions.Routing;

namespace Dim.Abstractions.Authentication;

public sealed record AuthenticationResult(
    string UserId,
    DimClientPlatform Platform);
