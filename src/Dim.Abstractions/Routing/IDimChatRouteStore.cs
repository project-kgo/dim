namespace Dim.Abstractions.Routing;

public interface IDimChatRouteStore
{
    ValueTask<DimChatRoute?> GetRouteAsync(
        DimChatRouteScope scope,
        CancellationToken cancellationToken);

    ValueTask SetRouteAsync(
        DimChatRoute route,
        TimeSpan ttl,
        CancellationToken cancellationToken);

    ValueTask<DimChatRouteScope?> GetRouteScopeAsync(
        string connectionId,
        CancellationToken cancellationToken);

    ValueTask<bool> RemoveRouteIfCurrentAsync(
        DimChatRouteScope scope,
        string connectionId,
        CancellationToken cancellationToken);

    ValueTask<bool> RefreshRouteAsync(
        DimChatRouteScope scope,
        string connectionId,
        TimeSpan ttl,
        CancellationToken cancellationToken);
}
