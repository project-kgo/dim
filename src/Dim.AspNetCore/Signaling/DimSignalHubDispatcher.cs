using Dim.Abstractions.Configuration;
using Dim.Application.Signaling;
using Dim.AspNetCore.Hubs;
using Dim.Contracts;
using Google.Protobuf;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System.Text;

namespace Dim.AspNetCore.Signaling;

public sealed class DimSignalHubDispatcher(
    IHubContext<DimChatHub> hubContext,
    IOptions<DimChatOptions> options) : IDimLocalSignalDispatcher
{
    private const string ForceOfflineClientMethod = "ForceOffline";

    private readonly IHubContext<DimChatHub> _hubContext = hubContext;
    private readonly string _clientMethodName = NormalizeClientMethodName(options.Value.Signaling.ClientMethodName);

    public async ValueTask DispatchAsync(
        SignalMessage signalMessage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(signalMessage);
        ArgumentNullException.ThrowIfNull(signalMessage.Envelope);

        var message = signalMessage.Envelope.ToByteArray();
        var target = signalMessage.Target ?? throw new InvalidOperationException("Dim 信令目标未设置。");

        switch (target.TargetCase)
        {
            case SignalTarget.TargetOneofCase.Users:
                ThrowIfInvalidAppId(target.AppId);
                var userIdentifiers = target.Users.UserIds
                    .Select(userId => DimSignalTargetNames.UserIdentifier(target.AppId, userId))
                    .ToArray();
                await _hubContext.Clients
                    .Users(userIdentifiers)
                    .SendAsync(_clientMethodName, message, cancellationToken);
                break;

            case SignalTarget.TargetOneofCase.Connections:
                await SendToConnectionsAsync(
                    target.Connections.ConnectionIds,
                    signalMessage.Envelope,
                    message,
                    cancellationToken);
                break;

            case SignalTarget.TargetOneofCase.All:
                ThrowIfInvalidAppId(target.AppId);
                await _hubContext.Clients
                    .Group(DimSignalTargetNames.AppGroup(target.AppId))
                    .SendAsync(_clientMethodName, message, cancellationToken);
                break;

            default:
                throw new InvalidOperationException("Dim 信令目标未设置。");
        }
    }

    private static void ThrowIfInvalidAppId(long appId)
    {
        if (appId <= 0)
        {
            throw new InvalidOperationException("Dim 信令目标 AppId 未设置。");
        }
    }

    private static string NormalizeClientMethodName(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "ReceiveSignal" : value.Trim();
    }

    private async ValueTask SendToConnectionsAsync(
        IReadOnlyList<string> connectionIds,
        SignalEnvelope envelope,
        byte[] message,
        CancellationToken cancellationToken)
    {
        if (envelope.SignalType == DimInternalSignalTypes.ForceOffline)
        {
            var reason = Encoding.UTF8.GetString(envelope.Payload.Span);
            await _hubContext.Clients
                .Clients(connectionIds)
                .SendAsync(ForceOfflineClientMethod, reason, cancellationToken);
            return;
        }

        await _hubContext.Clients
            .Clients(connectionIds)
            .SendAsync(_clientMethodName, message, cancellationToken);
    }
}
