using Dim.Abstractions.Routing;

namespace Dim.Infrastructure.Routing;

public class MissingDimRouteStore : IDimChatRouteStore
{
    public ValueTask<bool> RefreshRouteAsync(DimConectionRoute route, TimeSpan ttl, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<IReadOnlyCollection<DimUserConnectionRoute>> GetRoutesAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task RefreshTTLRoutesAsync(IEnumerable<DimConectionRoute> routes, TimeSpan ttl, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> RemoveRouteAsync(DimConectionRoute route, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<DimReplacedConnectionRoute[]?> SetRouteAsync(
        DimConectionRoute route,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
