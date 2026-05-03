namespace Dim.Abstractions.Routing;

public sealed record DimReplacedConnectionRoute(
    DimClientPlatform Platform,
    string ConnectionId,
    string ServerId);
