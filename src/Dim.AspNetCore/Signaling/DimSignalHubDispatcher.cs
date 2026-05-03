using Dim.Abstractions.Configuration;
using Dim.Application.Signaling;
using Dim.AspNetCore.Hubs;
using Dim.Contracts;
using Google.Protobuf;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Dim.AspNetCore.Signaling;

public sealed class DimSignalHubDispatcher(
    IHubContext<DimChatHub> hubContext,
    IOptions<DimChatOptions> options) : IDimLocalSignalDispatcher
{
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

            // case SignalTarget.TargetOneofCase.ConnectionId:
            //     await _hubContext.Clients
            //         .Client(target.ConnectionId)
            //         .SendAsync(_clientMethodName, message, cancellationToken);
            //     break;

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
}
