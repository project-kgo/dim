namespace Dim.Abstractions.Routing;

public interface IDimChatRouteStore
{
    ValueTask<DimReplacedConnectionRoute[]?> SetRouteAsync(
        DimConectionRoute route,
        TimeSpan ttl,
        CancellationToken cancellationToken);

    ValueTask<IReadOnlyCollection<DimUserConnectionRoute>> GetRoutesAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken);

    ValueTask<bool> RemoveRouteAsync(DimConectionRoute route, CancellationToken cancellationToken);

    ValueTask<bool> RefreshRouteAsync(DimConectionRoute route, TimeSpan ttl, CancellationToken cancellationToken);

    Task RefreshTTLRoutesAsync(
        IEnumerable<DimConectionRoute> routes,
        TimeSpan ttl,
        CancellationToken cancellationToken);
}
