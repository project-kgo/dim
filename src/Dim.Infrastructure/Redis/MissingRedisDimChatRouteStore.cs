using Dim.Abstractions.Routing;

namespace Dim.Infrastructure.Redis;

internal sealed class MissingRedisDimChatRouteStore : IDimChatRouteStore
{
    private const string Message = "DimChat:Storage:RedisConnectionString 未配置，无法保存用户路由。";

    public ValueTask<DimChatRoute?> GetRouteAsync(
        DimChatRouteScope scope,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(Message);
    }

    public ValueTask SetRouteAsync(
        DimChatRoute route,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(Message);
    }

    public ValueTask<DimChatRouteScope?> GetRouteScopeAsync(
        string connectionId,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(Message);
    }

    public ValueTask<bool> RemoveRouteIfCurrentAsync(
        DimChatRouteScope scope,
        string connectionId,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(Message);
    }

    public ValueTask<bool> RefreshRouteAsync(
        DimChatRouteScope scope,
        string connectionId,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(Message);
    }
}
