using Dim.Abstractions.Routing;

namespace Dim.AspNetCore.Authentication;

public sealed record DimChatAuthenticationResult(
    string UserId,
    DimClientPlatform Platform);
