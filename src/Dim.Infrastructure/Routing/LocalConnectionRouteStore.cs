using System.Collections.Concurrent;
using Dim.Abstractions.Routing;

namespace Dim.Infrastructure.Routing;

public class LocalConnectionRouteStore : ILocalConnectionRouteStore
{
    private readonly ConcurrentDictionary<string, DimConectionRoute> _connections = new();

    public void Add(DimConectionRoute route)
    {
        _connections[route.ConnectionId] = route;
    }

    public IReadOnlyCollection<DimConectionRoute> GetAll()
    {
        return [.. _connections.Values];
    }

    public bool TryGet(string connectionId, out DimConectionRoute? route)
    {
        return _connections.TryGetValue(connectionId, out route);
    }

    public void Remove(DimConectionRoute route)
    {
        _connections.TryRemove(route.ConnectionId, out _);
    }
}
