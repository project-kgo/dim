using System.Collections.Concurrent;
using Dim.Abstractions.Routing;

namespace Dim.Infrastructure.Routing;

public class LocalConnectionRouteStore : ILocalConnectionRouteStore
{
    private readonly ConcurrentDictionary<string, DimChatRoute> _connections = new();

    public void Add(DimChatRoute route)
    {
        _connections[route.ConnectionId] = route;
    }

    public IReadOnlyCollection<DimChatRoute> GetAll()
    {
        return [.. _connections.Values];
    }

    public void Remove(DimChatRoute route)
    {
        _connections.TryRemove(route.ConnectionId, out _);
    }
}
