namespace Dim.Abstractions.Routing;

public interface IDimChatRouteStore
{
    ValueTask<string[]?> SetRouteAsync(DimChatRoute route, TimeSpan ttl, CancellationToken cancellationToken);

    ValueTask<bool> RemoveRouteAsync(DimChatRoute route, CancellationToken cancellationToken);

    ValueTask<bool> RefreshRouteAsync(DimChatRoute route, TimeSpan ttl, CancellationToken cancellationToken);
}
