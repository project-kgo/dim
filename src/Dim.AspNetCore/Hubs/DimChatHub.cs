using Dim.Abstractions.Configuration;
using Dim.Abstractions.Routing;
using Dim.Application.Routing;
using Dim.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Dim.AspNetCore.Hubs;

public sealed class DimChatHub(
    IDimAuthenticator authenticator,
    DimChatRouteService routeService,
    IOptions<DimChatOptions> options) : Hub
{
    private const string ForceOfflineClientMethod = "ForceOffline";
    private const string ForceOfflineReason = "replaced";

    private readonly IDimAuthenticator _authenticator = authenticator;
    private readonly DimChatRouteService _routeService = routeService;
    private readonly DimChatConnectionOptions _connectionOptions = options.Value.Connection;

    public override async Task OnConnectedAsync()
    {
        var authenticationResult = await _authenticator.AuthenticateAsync(
            new DimAuthenticationContext(
                Context.GetHttpContext(),
                Context.User,
                Context.ConnectionId),
            Context.ConnectionAborted);

        if (!IsValid(authenticationResult))
        {
            Context.Abort();
            return;
        }

        var authenticatedUser = authenticationResult!;

        try
        {
            var routeResult = await _routeService.ConnectAsync(
                authenticatedUser.UserId,
                authenticatedUser.Platform,
                Context.ConnectionId,
                _connectionOptions,
                Context.ConnectionAborted);

            if (routeResult.ReplacedRoute is { } replacedRoute)
            {
                await Clients
                    .Client(replacedRoute.ConnectionId)
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

    public async ValueTask<bool> RefreshRouteAsync()
    {
        try
        {
            return await _routeService.RefreshRouteAsync(
                Context.ConnectionId,
                _connectionOptions.RouteTtl,
                Context.ConnectionAborted);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool IsValid(DimChatAuthenticationResult? authenticationResult)
    {
        return authenticationResult is not null
            && !string.IsNullOrWhiteSpace(authenticationResult.UserId)
            && Enum.IsDefined(authenticationResult.Platform);
    }
}
