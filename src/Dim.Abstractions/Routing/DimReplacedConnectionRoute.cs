namespace Dim.Abstractions.Routing;

public sealed record DimReplacedConnectionRoute(
    long AppId,
    DimClientPlatform Platform,
    string ConnectionId,
    string ServerId);
