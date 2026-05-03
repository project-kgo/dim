using Dim.Abstractions.Authentication;
using Dim.Abstractions.Configuration;
using Dim.Abstractions.Routing;
using Dim.Abstractions.Signaling;
using Dim.Application.Routing;
using Dim.Application.Signaling;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace Dim.AspNetCore.Hubs;

public sealed class DimChatHub(
    DimChatRouteService routeService,
    IDimSignalSender signalSender,
    ILogger<DimChatHub> logger,
    IOptions<DimChatOptions> options) : Hub
{
    private static readonly Action<ILogger, string, string, Exception?> LogForceOfflineFailed =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(1, nameof(LogForceOfflineFailed)),
            "发布旧连接下线信令失败，ServerId: {ServerId}, ConnectionId: {ConnectionId}。");

    private const string ForceOfflineReason = "replaced";

    private readonly DimChatRouteService _routeService = routeService;
    private readonly IDimSignalSender _signalSender = signalSender;
    private readonly ILogger<DimChatHub> _logger = logger;
    private readonly DimChatConnectionOptions _connectionOptions = options.Value.Connection;

    public override async Task OnConnectedAsync()
    {
        if (!TryGetAuthenticatedUser(out var userId, out var platform))
        {
            Context.Abort();
            return;
        }

        try
        {
            var routeResult = await _routeService.ConnectAsync(
                userId,
                platform,
                Context.ConnectionId,
                _connectionOptions,
                Context.ConnectionAborted);

            await SendForceOfflineAsync(routeResult.ReplacedRoutes, Context.ConnectionAborted);
        }
        catch (InvalidOperationException)
        {
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (!TryGetAuthenticatedUser(out _, out _))
        {
            Context.Abort();
            return;
        }
        try
        {
            await _routeService.DisconnectAsync(Context.ConnectionId, CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            // Redis 未配置或不可用时，连接阶段已经失败；断开阶段无需再次向外抛出。
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async ValueTask SendForceOfflineAsync(
        IReadOnlyCollection<DimReplacedConnectionRoute>? replacedRoutes,
        CancellationToken cancellationToken)
    {
        if (replacedRoutes is not { Count: > 0 })
        {
            return;
        }

        foreach (var route in replacedRoutes)
        {
            try
            {
                await _signalSender.SendToConnectionAsync(
                    route.ServerId,
                    route.ConnectionId,
                    DimInternalSignalTypes.ForceOffline,
                    Encoding.UTF8.GetBytes(ForceOfflineReason),
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                LogForceOfflineFailed(_logger, route.ServerId, route.ConnectionId, exception);
            }
        }
    }

    private bool TryGetAuthenticatedUser(
        out string userId,
        out DimClientPlatform platform)
    {
        userId = Context.User?.FindFirst(AuthConstants.UserIdClaim)?.Value ?? string.Empty;
        platform = default;

        var platformValue = Context.User?.FindFirst(AuthConstants.PlatformClaim)?.Value;

        return !string.IsNullOrWhiteSpace(userId)
            && Enum.TryParse(platformValue, ignoreCase: true, out platform)
            && Enum.IsDefined(platform);
    }
}
