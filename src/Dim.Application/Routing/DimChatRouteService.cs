using Dim.Abstractions.Configuration;
using Dim.Abstractions.Routing;

namespace Dim.Application.Routing;

public sealed class DimChatRouteService(IDimChatRouteStore routeStore, ILocalConnectionRouteStore localConnectionRouteStore)
{
    private readonly IDimChatRouteStore _routeStore = routeStore;
    private readonly ILocalConnectionRouteStore _localConnectionRouteStore = localConnectionRouteStore;

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

        _localConnectionRouteStore.Add(currentRoute);

        return new DimChatRouteConnectResult(currentRoute, previousConnectionIds);
    }

    public async ValueTask DisconnectAsync(DimChatRoute route, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(route.ConnectionId);

        await _routeStore.RemoveRouteAsync(route, cancellationToken);

        _localConnectionRouteStore.Remove(route);
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
