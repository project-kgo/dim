using Dim.Contracts;
using Google.Protobuf;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dim.Application.Signaling;

public sealed class DimSignalSubscriptionService(
    IDimSignalBus signalBus,
    IDimLocalSignalDispatcher localDispatcher,
    ILogger<DimSignalSubscriptionService> logger) : BackgroundService
{
    private static readonly TimeSpan SubscribeRetryDelay = TimeSpan.FromSeconds(5);
    private static readonly Action<ILogger, Exception?> LogSubscribeFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1, nameof(LogSubscribeFailed)),
            "订阅 Dim Redis 信令通道失败，将稍后重试。");
    private static readonly Action<ILogger, Exception?> LogInvalidMessage =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(2, nameof(LogInvalidMessage)),
            "收到无法解析的 Dim 信令消息，已跳过。");
    private static readonly Action<ILogger, string, Exception?> LogDispatchFailed =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(3, nameof(LogDispatchFailed)),
            "分发 Dim 信令消息失败，MessageId: {MessageId}。");

    private readonly IDimSignalBus _signalBus = signalBus;
    private readonly IDimLocalSignalDispatcher _localDispatcher = localDispatcher;
    private readonly ILogger<DimSignalSubscriptionService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var subscription = await _signalBus.SubscribeAsync(HandleMessageAsync, stoppingToken);
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                LogSubscribeFailed(_logger, exception);

                try
                {
                    await Task.Delay(SubscribeRetryDelay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                }
            }
        }
    }

    private async ValueTask HandleMessageAsync(
        ReadOnlyMemory<byte> message,
        CancellationToken cancellationToken)
    {
        SignalEnvelope envelope;

        try
        {
            envelope = SignalEnvelope.Parser.ParseFrom(message.Span);
        }
        catch (InvalidProtocolBufferException exception)
        {
            LogInvalidMessage(_logger, exception);
            return;
        }

        try
        {
            await _localDispatcher.DispatchAsync(envelope, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            LogDispatchFailed(_logger, envelope.MessageId, exception);
        }
    }
}
