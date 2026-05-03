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
                await _hubContext.Clients
                    .Users(target.Users.UserIds)
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
                await _hubContext.Clients
                    .All
                    .SendAsync(_clientMethodName, message, cancellationToken);
                break;

            default:
                throw new InvalidOperationException("Dim 信令目标未设置。");
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
