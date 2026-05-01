namespace Dim.Abstractions.Routing;

public sealed record DimChatRoute(
    string UserId,
    DimClientPlatform Platform,
    string ConnectionId,
    DateTimeOffset ConnectedAtUtc);
