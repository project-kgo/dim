namespace Dim.Abstractions.Signaling;

public interface IDimSignalSender
{
    ValueTask SendToUsersAsync(
        IReadOnlyCollection<string> userIds,
        string signalType,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default);

    ValueTask SendToConnectionAsync(
        string connectionId,
        string signalType,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default);

    ValueTask BroadcastAsync(
        string signalType,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default);
}
