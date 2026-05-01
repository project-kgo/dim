using Dim.Abstractions.Configuration;
using Dim.Abstractions.Routing;

namespace Dim.Application.Routing;

public sealed class DimChatRouteService(IDimChatRouteStore routeStore)
{
    private readonly IDimChatRouteStore _routeStore = routeStore;

    public async ValueTask<DimChatRouteConnectResult> ConnectAsync(
        string userId,
        DimClientPlatform platform,
        string connectionId,
        DimChatConnectionOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentNullException.ThrowIfNull(options);

        var currentRoute = new DimChatRoute(
            userId,
            platform,
            connectionId,
            DateTimeOffset.UtcNow);

        var previousConnectionIds = await _routeStore.SetRouteAsync(currentRoute, options.RouteTtl, cancellationToken);

        return new DimChatRouteConnectResult(currentRoute, previousConnectionIds);
    }

    public async ValueTask DisconnectAsync(DimChatRoute route, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(route.ConnectionId);

        await _routeStore.RemoveRouteAsync(route, cancellationToken);
    }

    public async ValueTask<bool> RefreshRouteAsync(
        DimChatRoute route,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(route.ConnectionId);

        return await _routeStore.RefreshRouteAsync(route, ttl, cancellationToken);
    }
}
