using Dim.Abstractions.Routing;

namespace Dim.UnitTests.Routing;

internal sealed class TestDimChatRouteStore(bool allowMultiDeviceLogin = true) : IDimChatRouteStore
{
    private readonly Dictionary<string, DimChatRoute> _routes = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, DimChatRoute> Routes => _routes;

    public int RefreshCount { get; private set; }

    public ValueTask<string[]?> SetRouteAsync(
        DimChatRoute route,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        var key = GetRouteKey(route);
        var previousConnectionIds = GetPreviousConnectionIds(key);

        if (!allowMultiDeviceLogin)
        {
            _routes.Clear();
        }

        _routes[key] = route;

        return ValueTask.FromResult(previousConnectionIds);
    }

    public ValueTask<bool> RemoveRouteAsync(
        DimChatRoute route,
        CancellationToken cancellationToken)
    {
        var key = GetRouteKey(route);
        if (!_routes.TryGetValue(key, out var currentRoute)
            || currentRoute.ConnectionId != route.ConnectionId)
        {
            return ValueTask.FromResult(false);
        }

        _routes.Remove(key);
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> RefreshRouteAsync(
        DimChatRoute route,
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

    private string[]? GetPreviousConnectionIds(string routeKey)
    {
        var connectionIds = allowMultiDeviceLogin
            ? GetRouteConnectionIds(routeKey)
            : [.. _routes.Values.Select(route => route.ConnectionId)];

        return connectionIds.Length == 0 ? null : connectionIds;
    }

    private string[] GetRouteConnectionIds(string routeKey)
    {
        return _routes.TryGetValue(routeKey, out var route)
            ? [route.ConnectionId]
            : [];
    }

    private string GetRouteKey(DimChatRoute route)
    {
        return allowMultiDeviceLogin
            ? $"user:{route.UserId}:platform:{route.Platform.ToString().ToLowerInvariant()}"
            : $"user:{route.UserId}";
    }
}
