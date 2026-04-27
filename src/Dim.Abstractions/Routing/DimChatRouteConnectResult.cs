namespace Dim.Abstractions.Routing;

public sealed record DimChatRouteConnectResult(
    DimChatRoute CurrentRoute,
    DimChatRoute? ReplacedRoute);
