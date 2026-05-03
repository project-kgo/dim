using Dim.Abstractions.Signaling;
using Dim.Abstractions.Routing;
using Dim.Contracts;
using Google.Protobuf;

namespace Dim.Application.Signaling;

public sealed class DimSignalSender(
    IDimSignalBus signalBus,
    IDimChatRouteStore routeStore) : IDimSignalSender
{
    private readonly IDimSignalBus _signalBus = signalBus;
    private readonly IDimChatRouteStore _routeStore = routeStore;

    public async ValueTask SendToUsersAsync(
        IReadOnlyCollection<string> userIds,
        string signalType,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default)
    {
        var normalizedUserIds = NormalizeUserIds(userIds);
        var envelope = CreateSignalEnvelope(signalType, payload);
        var routes = await _routeStore.GetRoutesAsync(normalizedUserIds, cancellationToken);

        foreach (var group in routes.GroupBy(route => route.ServerId, StringComparer.Ordinal))
        {
            var connectionIds = group
                .Select(route => route.ConnectionId)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (connectionIds.Length == 0)
            {
                continue;
            }

            await PublishToServerAsync(
                group.Key,
                envelope,
                new SignalTarget
                {
                    Connections = new SignalConnectionTarget
                    {
                        ConnectionIds = { connectionIds }
                    }
                },
                cancellationToken);
        }
    }

    public ValueTask SendToConnectionAsync(
        string serverId,
        string connectionId,
        string signalType,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverId);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        var envelope = CreateSignalEnvelope(signalType, payload);
        return PublishToServerAsync(
            serverId,
            envelope,
            new SignalTarget
            {
                Connections = new SignalConnectionTarget
                {
                    ConnectionIds = { connectionId.Trim() }
                }
            },
            cancellationToken);
    }

    public ValueTask BroadcastAsync(
        string signalType,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signalType);

        var signalMessage = CreateSignalMessage(
            CreateSignalEnvelope(signalType, payload),
            new SignalTarget { All = true });
        return _signalBus.PublishAsync(signalMessage.ToByteArray(), cancellationToken);
    }

    private ValueTask PublishToServerAsync(
        string serverId,
        SignalEnvelope envelope,
        SignalTarget target,
        CancellationToken cancellationToken)
    {
        var signalMessage = CreateSignalMessage(envelope, target);
        return _signalBus.PublishToServerAsync(serverId, signalMessage.ToByteArray(), cancellationToken);
    }

    private static SignalEnvelope CreateSignalEnvelope(
        string signalType,
        ReadOnlyMemory<byte> payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signalType);

        return new SignalEnvelope
        {
            MessageId = Guid.NewGuid().ToString("N"),
            SignalType = signalType,
            Payload = ByteString.CopyFrom(payload.Span),
            SentAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    private static SignalMessage CreateSignalMessage(
        SignalEnvelope envelope,
        SignalTarget target)
    {
        return new SignalMessage
        {
            Target = target,
            Envelope = envelope,
        };
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
