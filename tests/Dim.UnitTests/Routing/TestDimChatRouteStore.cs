using Dim.Abstractions.Routing;

namespace Dim.UnitTests.Routing;

internal sealed class TestDimChatRouteStore(bool allowMultiDeviceLogin = true) : IDimChatRouteStore
{
    private readonly Dictionary<string, DimConectionRoute> _routes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _serverRoutes = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, DimConectionRoute> Routes => _routes;

    public IReadOnlyDictionary<string, string> ServerRoutes => _serverRoutes;

    public int RefreshCount { get; private set; }

    public ValueTask<DimReplacedConnectionRoute[]?> SetRouteAsync(
        DimConectionRoute route,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        var key = GetRouteKey(route);
        var replacedRoutes = GetReplacedRoutes(key);

        _routes[key] = route;
        _serverRoutes[key] = route.ServerId;

        return ValueTask.FromResult(replacedRoutes);
    }

    public ValueTask<IReadOnlyCollection<DimUserConnectionRoute>> GetRoutesAsync(
        long appId,
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken)
    {
        var normalizedUserIds = userIds.ToHashSet(StringComparer.Ordinal);
        var routes = _routes.Values
            .Where(route => route.AppId == appId && normalizedUserIds.Contains(route.UserId))
            .Select(route => new DimUserConnectionRoute(
                route.AppId,
                route.UserId,
                route.Platform,
                route.ConnectionId,
                route.ServerId))
            .ToArray();

        return ValueTask.FromResult<IReadOnlyCollection<DimUserConnectionRoute>>(routes);
    }

    public ValueTask<bool> RemoveRouteAsync(
        DimConectionRoute route,
        CancellationToken cancellationToken)
    {
        var key = GetRouteKey(route);
        if (!_routes.TryGetValue(key, out var currentRoute)
            || currentRoute.ConnectionId != route.ConnectionId)
        {
            return ValueTask.FromResult(false);
        }

        _routes.Remove(key);
        _serverRoutes.Remove(key);
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> RefreshRouteAsync(
        DimConectionRoute route,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        if (!_routes.TryGetValue(GetRouteKey(route), out var currentRoute)
            || currentRoute.ConnectionId != route.ConnectionId)
        {
            return ValueTask.FromResult(false);
        }

        RefreshCount++;
        return ValueTask.FromResult(true);
    }

    public Task RefreshTTLRoutesAsync(
        IEnumerable<DimConectionRoute> routes,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        foreach (var route in routes)
        {
            if (_routes.TryGetValue(GetRouteKey(route), out var currentRoute)
                && currentRoute.ConnectionId == route.ConnectionId)
            {
                RefreshCount++;
            }
        }

        return Task.CompletedTask;
    }

    private DimReplacedConnectionRoute[]? GetReplacedRoutes(string routeKey)
    {
        var routes = GetRouteReplacedRoutes(routeKey);

        return routes.Length == 0 ? null : routes;
    }

    private DimReplacedConnectionRoute[] GetRouteReplacedRoutes(string routeKey)
    {
        return _routes.TryGetValue(routeKey, out var route)
            ? [ToReplacedRoute(route)]
            : [];
    }

    private static DimReplacedConnectionRoute ToReplacedRoute(DimConectionRoute route)
    {
        return new DimReplacedConnectionRoute(route.AppId, route.Platform, route.ConnectionId, route.ServerId);
    }

    private string GetRouteKey(DimConectionRoute route)
    {
        return allowMultiDeviceLogin
            ? $"app:{route.AppId}:user:{route.UserId}:platform:{route.Platform.ToString().ToLowerInvariant()}"
            : $"app:{route.AppId}:user:{route.UserId}";
    }
}

internal sealed class TestLocalConnectionRouteStore : ILocalConnectionRouteStore
{
    private readonly Dictionary<string, DimConectionRoute> _routes = new(StringComparer.Ordinal);

    public int Count => _routes.Count;

    public void Add(DimConectionRoute route)
    {
        _routes[route.ConnectionId] = route;
    }

    public void Remove(DimConectionRoute route)
    {
        _routes.Remove(route.ConnectionId);
    }

    public bool TryGet(string connectionId, out DimConectionRoute? route)
    {
        return _routes.TryGetValue(connectionId, out route);
    }

    public IReadOnlyCollection<DimConectionRoute> GetAll()
    {
        return [.. _routes.Values];
    }

    public IReadOnlyCollection<DimConectionRoute> GetRefreshBatch(int maxCount)
    {
        return [.. _routes.Values.Take(maxCount)];
    }

    public void MoveToTail(IReadOnlyCollection<DimConectionRoute> routes)
    {
    }
}
