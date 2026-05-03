using Dim.Abstractions.Configuration;
using Dim.Abstractions.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Dim.Application.Routing;

public class OnlineTTLRefreshService(
    ILocalConnectionRouteStore localStore,
    IDimChatRouteStore chatRouteStore,
    IOptions<DimChatOptions> options) : BackgroundService
{
    private readonly ILocalConnectionRouteStore _localStore = localStore;
    private readonly IDimChatRouteStore _chatRouteStore = chatRouteStore;

    private readonly TimeSpan _ttl = options.Value.Connection.RouteTtl;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var routes = _localStore.GetAll();
                await _chatRouteStore.RefreshTTLRoutesAsync(routes, _ttl, stoppingToken);
            }

            // temp
            
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
