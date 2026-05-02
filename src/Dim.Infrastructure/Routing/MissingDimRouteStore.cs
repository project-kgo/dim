using Dim.Abstractions.Routing;

namespace Dim.Infrastructure.Routing;

public class MissingDimRouteStore : IDimChatRouteStore
{
    public ValueTask<bool> RefreshRouteAsync(DimChatRoute route, TimeSpan ttl, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task RefreshTTLRoutesAsync(IEnumerable<DimChatRoute> routes, TimeSpan ttl, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> RemoveRouteAsync(DimChatRoute route, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask<string[]?> SetRouteAsync(DimChatRoute route, TimeSpan ttl, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
