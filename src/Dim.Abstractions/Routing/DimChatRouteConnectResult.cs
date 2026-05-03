namespace Dim.Abstractions.Routing;

public sealed record DimChatRouteConnectResult(
    DimConectionRoute CurrentRoute,
    IReadOnlyCollection<DimReplacedConnectionRoute>? ReplacedRoutes);
