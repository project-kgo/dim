using Dim.Abstractions.Configuration;
using Dim.Application.Signaling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Dim.Infrastructure.Signaling;

public sealed class RedisDimSignalBus(
    IConnectionMultiplexer connectionMultiplexer,
    IOptions<DimChatOptions> options,
    ILogger<RedisDimSignalBus> logger) : IDimSignalBus
{
    private static readonly Action<ILogger, Exception?> LogHandleMessageFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1, nameof(LogHandleMessageFailed)),
            "处理 Redis Dim 信令消息失败。");

    private readonly ISubscriber _subscriber = connectionMultiplexer.GetSubscriber();
    private readonly RedisChannel _channel = RedisChannel.Literal(NormalizeChannel(options.Value.Signaling.RedisChannel));
    private readonly ILogger<RedisDimSignalBus> _logger = logger;

    public async ValueTask PublishAsync(
        ReadOnlyMemory<byte> message,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _subscriber.PublishAsync(_channel, message.ToArray());
    }

    public async ValueTask<IDimSignalSubscription> SubscribeAsync(
        Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> handler,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        void OnMessage(RedisChannel channel, RedisValue value)
        {
            if (!value.HasValue)
            {
                return;
            }

            var bytes = (byte[])value!;
            _ = Task.Run(async () =>
            {
                try
                {
                    await handler(bytes, CancellationToken.None);
                }
                catch (Exception exception)
                {
                    LogHandleMessageFailed(_logger, exception);
                }
            }, CancellationToken.None);
        }

        await _subscriber.SubscribeAsync(_channel, OnMessage);

        return new RedisDimSignalSubscription(_subscriber, _channel, OnMessage);
    }

    private static string NormalizeChannel(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "dim:signals" : value.Trim();
    }

    private sealed class RedisDimSignalSubscription(
        ISubscriber subscriber,
        RedisChannel channel,
        Action<RedisChannel, RedisValue> handler) : IDimSignalSubscription
    {
        private readonly ISubscriber _subscriber = subscriber;
        private readonly RedisChannel _channel = channel;
        private readonly Action<RedisChannel, RedisValue> _handler = handler;

        public async ValueTask DisposeAsync()
        {
            await _subscriber.UnsubscribeAsync(_channel, _handler);
        }
    }
}
