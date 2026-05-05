using Dim.Abstractions.Configuration;
using Dim.Abstractions.Routing;

namespace Dim.Application.Routing;

public sealed class DimChatRouteService(
    IDimChatRouteStore routeStore,
    ILocalConnectionRouteStore localConnectionRouteStore,
    DimServerIdentity serverIdentity)
{
    private readonly IDimChatRouteStore _routeStore = routeStore;
    private readonly ILocalConnectionRouteStore _localConnectionRouteStore = localConnectionRouteStore;
    private readonly DimServerIdentity _serverIdentity = serverIdentity;

    public async ValueTask<DimChatRouteConnectResult> ConnectAsync(
        long appId,
        string userId,
        DimClientPlatform platform,
        string connectionId,
        DimChatConnectionOptions options,
        CancellationToken cancellationToken)
    {
        if (appId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(appId), "AppId 必须大于 0。");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentNullException.ThrowIfNull(options);

        var currentRoute = new DimConectionRoute(
            appId,
            userId,
            platform,
            connectionId,
            _serverIdentity.ServerId,
            DateTimeOffset.UtcNow);

        var replacedRoutes = await _routeStore.SetRouteAsync(currentRoute, options.RouteTtl, cancellationToken);

        _localConnectionRouteStore.Add(currentRoute);

        return new DimChatRouteConnectResult(currentRoute, replacedRoutes);
    }

    public async ValueTask DisconnectAsync(DimConectionRoute route, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(route.ConnectionId);

        await _routeStore.RemoveRouteAsync(route, cancellationToken);

        _localConnectionRouteStore.Remove(route);
    }

    public async ValueTask DisconnectAsync(string connectionId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        if (!_localConnectionRouteStore.TryGet(connectionId, out var route) || route is null)
        {
            return;
        }

        await DisconnectAsync(route, cancellationToken);
    }

    public async ValueTask<bool> RefreshRouteAsync(
        DimConectionRoute route,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(route.ConnectionId);

        return await _routeStore.RefreshRouteAsync(route, ttl, cancellationToken);
    }
}
