namespace Dim.Abstractions.Routing;

public sealed record DimChatRoute(
    DimChatRouteScope Scope,
    string UserId,
    DimClientPlatform Platform,
    string ConnectionId,
    DateTimeOffset ConnectedAtUtc);
