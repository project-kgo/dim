namespace Dim.Abstractions.Routing;

public sealed record DimConectionRoute(
    long AppId,
    string UserId,
    DimClientPlatform Platform,
    string ConnectionId,
    string ServerId,
    DateTimeOffset ConnectedAtUtc
);
