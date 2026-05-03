namespace Dim.Abstractions.Routing;

public sealed record DimConectionRoute(
    string UserId,
    DimClientPlatform Platform,
    string ConnectionId,
    string ServerId,
    DateTimeOffset ConnectedAtUtc
);
