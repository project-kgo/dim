namespace Dim.Abstractions.Signaling;

public interface IDimSignalSender
{
    ValueTask SendToUsersAsync(
        long appId,
        IReadOnlyCollection<string> userIds,
        string signalType,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default);

    ValueTask SendToConnectionAsync(
        long appId,
        string serverId,
        string connectionId,
        string signalType,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default);

    ValueTask BroadcastAsync(
        long appId,
        string signalType,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default);
}
