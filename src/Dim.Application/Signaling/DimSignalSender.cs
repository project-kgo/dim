using Dim.Abstractions.Signaling;
using Dim.Contracts;
using Google.Protobuf;

namespace Dim.Application.Signaling;

public sealed class DimSignalSender(IDimSignalBus signalBus) : IDimSignalSender
{
    private readonly IDimSignalBus _signalBus = signalBus;

    public ValueTask SendToUsersAsync(
        IReadOnlyCollection<string> userIds,
        string signalType,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default)
    {
        var normalizedUserIds = NormalizeUserIds(userIds);

        return PublishAsync(
            signalType,
            payload,
            new SignalTarget
            {
                Users = new SignalUserTarget
                {
                    UserIds = { normalizedUserIds }
                }
            },
            cancellationToken);
    }

    // public ValueTask SendToConnectionAsync(
    //     string connectionId,
    //     string signalType,
    //     ReadOnlyMemory<byte> payload,
    //     CancellationToken cancellationToken = default)
    // {
    //     ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

    //     return PublishAsync(
    //         signalType,
    //         payload,
    //         new SignalTarget { ConnectionId = connectionId },
    //         cancellationToken);
    // }

    public ValueTask BroadcastAsync(
        string signalType,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default)
    {
        return PublishAsync(
            signalType,
            payload,
            new SignalTarget { All = true },
            cancellationToken);
    }

    private ValueTask PublishAsync(
        string signalType,
        ReadOnlyMemory<byte> payload,
        SignalTarget target,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signalType);

        var signalMessage = new SignalMessage
        {
            Target = target,
            Envelope = new SignalEnvelope
            {
                MessageId = Guid.NewGuid().ToString("N"),
                SignalType = signalType,
                Payload = ByteString.CopyFrom(payload.Span),
                SentAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }
        };

        return _signalBus.PublishAsync(signalMessage.ToByteArray(), cancellationToken);
    }

    private static string[] NormalizeUserIds(IReadOnlyCollection<string> userIds)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        if (userIds.Count == 0)
        {
            throw new ArgumentException("用户列表不能为空。", nameof(userIds));
        }

        var normalizedUserIds = userIds
            .Select(userId =>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userIds));
                return userId.Trim();
            })
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalizedUserIds.Length == 0)
        {
            throw new ArgumentException("用户列表不能为空。", nameof(userIds));
        }

        return normalizedUserIds;
    }
}
