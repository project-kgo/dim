namespace Dim.Abstractions.Routing;

public sealed record DimUserConnectionRoute(
    long AppId,
    string UserId,
    DimClientPlatform Platform,
    string ConnectionId,
    string ServerId);
