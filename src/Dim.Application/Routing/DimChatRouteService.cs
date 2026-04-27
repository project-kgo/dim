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

        var scope = CreateScope(userId, platform, options);
        var previousRoute = await _routeStore.GetRouteAsync(scope, cancellationToken);
        var currentRoute = new DimChatRoute(
            scope,
            userId,
            platform,
            connectionId,
            DateTimeOffset.UtcNow);

        await _routeStore.SetRouteAsync(currentRoute, options.RouteTtl, cancellationToken);

        var replacedRoute = previousRoute?.ConnectionId == connectionId ? null : previousRoute;
        return new DimChatRouteConnectResult(currentRoute, replacedRoute);
    }

    public async ValueTask DisconnectAsync(string connectionId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        var scope = await _routeStore.GetRouteScopeAsync(connectionId, cancellationToken);
        if (scope is null)
        {
            return;
        }

        await _routeStore.RemoveRouteIfCurrentAsync(scope, connectionId, cancellationToken);
    }

    public async ValueTask<bool> RefreshRouteAsync(
        string connectionId,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        var scope = await _routeStore.GetRouteScopeAsync(connectionId, cancellationToken);
        if (scope is null)
        {
            return false;
        }

        return await _routeStore.RefreshRouteAsync(scope, connectionId, ttl, cancellationToken);
    }

    private static DimChatRouteScope CreateScope(
        string userId,
        DimClientPlatform platform,
        DimChatConnectionOptions options)
    {
        return options.AllowMultiDeviceLogin
            ? DimChatRouteScope.ForPlatform(userId, platform)
            : DimChatRouteScope.ForUser(userId);
    }
}
