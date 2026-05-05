using Dim.Abstractions.Routing;

namespace Dim.Infrastructure.Routing;

public class LocalConnectionRouteStore : ILocalConnectionRouteStore
{
    private readonly Lock _syncRoot = new();
    private readonly Dictionary<string, LinkedListNode<DimConectionRoute>> _connections = new(StringComparer.Ordinal);
    private readonly LinkedList<DimConectionRoute> _connectionOrder = new();

    public int Count
    {
        get
        {
            lock (_syncRoot)
            {
                return _connections.Count;
            }
        }
    }

    public void Add(DimConectionRoute route)
    {
        lock (_syncRoot)
        {
            if (_connections.TryGetValue(route.ConnectionId, out var node))
            {
                node.Value = route;
                _connectionOrder.Remove(node);
                _connectionOrder.AddLast(node);
                return;
            }

            node = _connectionOrder.AddLast(route);
            _connections.Add(route.ConnectionId, node);
        }
    }

    public IReadOnlyCollection<DimConectionRoute> GetAll()
    {
        lock (_syncRoot)
        {
            return [.. _connectionOrder];
        }
    }

    public IReadOnlyCollection<DimConectionRoute> GetRefreshBatch(int maxCount)
    {
        if (maxCount <= 0)
        {
            return [];
        }

        lock (_syncRoot)
        {
            if (_connectionOrder.Count == 0)
            {
                return [];
            }

            var routes = new List<DimConectionRoute>(Math.Min(maxCount, _connectionOrder.Count));
            var current = _connectionOrder.First;
            while (current is not null && routes.Count < maxCount)
            {
                routes.Add(current.Value);
                current = current.Next;
            }

            return routes;
        }
    }

    public bool TryGet(string connectionId, out DimConectionRoute? route)
    {
        lock (_syncRoot)
        {
            if (_connections.TryGetValue(connectionId, out var node))
            {
                route = node.Value;
                return true;
            }

            route = null;
            return false;
        }
    }

    public void Remove(DimConectionRoute route)
    {
        lock (_syncRoot)
        {
            if (!_connections.Remove(route.ConnectionId, out var node))
            {
                return;
            }

            _connectionOrder.Remove(node);
        }
    }

    public void MoveToTail(IReadOnlyCollection<DimConectionRoute> routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        if (routes.Count == 0)
        {
            return;
        }

        lock (_syncRoot)
        {
            foreach (var route in routes)
            {
                if (!_connections.TryGetValue(route.ConnectionId, out var node)
                    || node.List is null
                    || node.Value != route)
                {
                    continue;
                }

                _connectionOrder.Remove(node);
                _connectionOrder.AddLast(node);
            }
        }
    }
}
