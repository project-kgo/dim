namespace Dim.Abstractions.Routing;

public sealed record DimUserConnectionRoute(
    string UserId,
    DimClientPlatform Platform,
    string ConnectionId,
    string ServerId);
