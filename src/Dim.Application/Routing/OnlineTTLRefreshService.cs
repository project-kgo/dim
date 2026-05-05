using Dim.Abstractions.Configuration;
using Dim.Abstractions.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dim.Application.Routing;

public class OnlineTTLRefreshService(
    ILocalConnectionRouteStore localStore,
    IDimChatRouteStore chatRouteStore,
    ILogger<OnlineTTLRefreshService> logger,
    IOptions<DimChatOptions> options) : BackgroundService
{
    private const int MaxRefreshBatchSize = 512;
    private const double RefreshWindowRatio = 0.8;

    private static readonly TimeSpan DefaultTtl = TimeSpan.FromDays(7);
    private static readonly TimeSpan MaxRefreshInterval = TimeSpan.FromSeconds(30);

    private static readonly Action<ILogger, int, Exception?> LogRefreshFailed =
        LoggerMessage.Define<int>(
            LogLevel.Warning,
            new EventId(1, nameof(LogRefreshFailed)),
            "刷新本地连接 Redis TTL 失败，连接数量: {RouteCount}。");

    private readonly ILocalConnectionRouteStore _localStore = localStore;
    private readonly IDimChatRouteStore _chatRouteStore = chatRouteStore;
    private readonly ILogger<OnlineTTLRefreshService> _logger = logger;

    private readonly TimeSpan _ttl = options.Value.Connection.RouteTtl;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var schedule = CalculateRefreshSchedule(_localStore.Count, _ttl);
                await Task.Delay(schedule.Delay, stoppingToken);

                if (schedule.BatchSize <= 0)
                {
                    continue;
                }

                var routes = _localStore.GetRefreshBatch(schedule.BatchSize);
                if (routes.Count == 0)
                {
                    continue;
                }

                try
                {
                    await _chatRouteStore.RefreshTTLRoutesAsync(routes, _ttl, stoppingToken);
                    _localStore.MoveToTail(routes);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    LogRefreshFailed(_logger, routes.Count, exception);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    internal static OnlineTtlRefreshSchedule CalculateRefreshSchedule(int connectionCount, TimeSpan ttl)
    {
        var refreshWindow = GetRefreshWindow(ttl);
        if (connectionCount <= 0)
        {
            return new OnlineTtlRefreshSchedule(
                Min(refreshWindow, MaxRefreshInterval),
                0,
                refreshWindow,
                1);
        }

        var tickCountByBatch = CeilingDivide(connectionCount, MaxRefreshBatchSize);
        var tickCountByInterval = CeilingDivide(refreshWindow.Ticks, MaxRefreshInterval.Ticks);
        var tickCount = Math.Max(Math.Max(tickCountByBatch, tickCountByInterval), 1);
        var batchSize = checked((int)Math.Max(1, CeilingDivide(connectionCount, tickCount)));
        var delayTicks = Math.Max(1, refreshWindow.Ticks / tickCount);

        return new OnlineTtlRefreshSchedule(
            TimeSpan.FromTicks(delayTicks),
            batchSize,
            refreshWindow,
            tickCount);
    }

    private static TimeSpan GetRefreshWindow(TimeSpan ttl)
    {
        var normalizedTtl = ttl > TimeSpan.Zero ? ttl : DefaultTtl;
        var refreshWindowTicks = Math.Max(1, checked((long)Math.Ceiling(normalizedTtl.Ticks * RefreshWindowRatio)));
        return TimeSpan.FromTicks(refreshWindowTicks);
    }

    private static long CeilingDivide(long dividend, long divisor)
    {
        return 1 + ((dividend - 1) / divisor);
    }

    private static TimeSpan Min(TimeSpan left, TimeSpan right)
    {
        return left <= right ? left : right;
    }
}

internal readonly record struct OnlineTtlRefreshSchedule(
    TimeSpan Delay,
    int BatchSize,
    TimeSpan RefreshWindow,
    long TickCount);
