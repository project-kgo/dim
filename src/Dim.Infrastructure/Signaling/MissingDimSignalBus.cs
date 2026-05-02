using Dim.Application.Signaling;

namespace Dim.Infrastructure.Signaling;

public sealed class MissingDimSignalBus : IDimSignalBus
{
    private static readonly IDimSignalSubscription Subscription = new MissingDimSignalSubscription();

    public ValueTask PublishAsync(
        ReadOnlyMemory<byte> message,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("DimChat:Storage:RedisConnectionString 未配置，无法发送 Dim 信令。");
    }

    public ValueTask<IDimSignalSubscription> SubscribeAsync(
        Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> handler,
        CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(Subscription);
    }

    private sealed class MissingDimSignalSubscription : IDimSignalSubscription
    {
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
