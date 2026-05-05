using Dim.Abstractions.Routing;
using Microsoft.Extensions.Hosting;

namespace Dim.Infrastructure.Routing;

internal sealed class DimRouteStoreScriptPreloadService(IDimChatRouteStore routeStore) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return routeStore is DimRouteStore dimRouteStore
            ? dimRouteStore.LoadRefreshScriptAsync(cancellationToken)
            : Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
