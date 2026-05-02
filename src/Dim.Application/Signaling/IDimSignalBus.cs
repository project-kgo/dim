namespace Dim.Application.Signaling;

public interface IDimSignalBus
{
    ValueTask PublishAsync(
        ReadOnlyMemory<byte> message,
        CancellationToken cancellationToken);

    ValueTask<IDimSignalSubscription> SubscribeAsync(
        Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> handler,
        CancellationToken cancellationToken);
}
