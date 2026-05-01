using Dim.Abstractions.Authentication;
using Dim.Abstractions.Configuration;
using Dim.Abstractions.Routing;
using Dim.Application.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Dim.AspNetCore.Hubs;

public sealed class DimChatHub(
    DimChatRouteService routeService,
    IOptions<DimChatOptions> options) : Hub
{
    private const string ForceOfflineClientMethod = "ForceOffline";
    private const string ForceOfflineReason = "replaced";

    private readonly DimChatRouteService _routeService = routeService;
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

            if (routeResult.PreviousConnectionIds is { } previousConnectionIds)
            {
                await Clients
                    .Clients(previousConnectionIds)
                    .SendAsync(
                        ForceOfflineClientMethod,
                        ForceOfflineReason,
                        Context.ConnectionAborted);
            }
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
        if (!TryGetAuthenticatedUser(out var userId, out var platform))
        {
            Context.Abort();
            return;
        }
        var route = new DimChatRoute(userId, platform, Context.ConnectionId, DateTimeOffset.UtcNow);
        try
        {
            await _routeService.DisconnectAsync(route, CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            // Redis 未配置或不可用时，连接阶段已经失败；断开阶段无需再次向外抛出。
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task HeartbeatAsync()
    {
        await Task.CompletedTask;
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
