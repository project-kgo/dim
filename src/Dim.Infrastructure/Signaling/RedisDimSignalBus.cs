using Dim.Abstractions.Configuration;
using Dim.Application.Routing;
using Dim.Application.Signaling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Dim.Infrastructure.Signaling;

public sealed class RedisDimSignalBus(
    IConnectionMultiplexer connectionMultiplexer,
    IOptions<DimChatOptions> options,
    DimServerIdentity serverIdentity,
    ILogger<RedisDimSignalBus> logger) : IDimSignalBus
{
    private static readonly Action<ILogger, Exception?> LogHandleMessageFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1, nameof(LogHandleMessageFailed)),
            "处理 Redis Dim 信令消息失败。");

    private readonly ISubscriber _subscriber = connectionMultiplexer.GetSubscriber();
    private readonly string _baseChannel = NormalizeChannel(options.Value.Signaling.RedisChannel);
    private readonly RedisChannel _channel = RedisChannel.Literal(NormalizeChannel(options.Value.Signaling.RedisChannel));
    private readonly RedisChannel _serverChannel = RedisChannel.Literal(ServerChannel(
        NormalizeChannel(options.Value.Signaling.RedisChannel),
        serverIdentity.ServerId));
    private readonly ILogger<RedisDimSignalBus> _logger = logger;

    public async ValueTask PublishAsync(
        ReadOnlyMemory<byte> message,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _subscriber.PublishAsync(_channel, message.ToArray());
    }

    public async ValueTask PublishToServerAsync(
        string serverId,
        ReadOnlyMemory<byte> message,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverId);
        cancellationToken.ThrowIfCancellationRequested();

        var channel = RedisChannel.Literal(ServerChannel(_baseChannel, serverId));
        await _subscriber.PublishAsync(channel, message.ToArray());
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

        var subscribedChannels = new List<RedisChannel>(capacity: 2);
        try
        {
            await _subscriber.SubscribeAsync(_channel, OnMessage);
            subscribedChannels.Add(_channel);

            await _subscriber.SubscribeAsync(_serverChannel, OnMessage);
            subscribedChannels.Add(_serverChannel);
        }
        catch
        {
            await UnsubscribeAsync(_subscriber, subscribedChannels, OnMessage);
            throw;
        }

        return new RedisDimSignalSubscription(_subscriber, [.. subscribedChannels], OnMessage);
    }

    private static string NormalizeChannel(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "dim:signals" : value.Trim();
    }

    private static string ServerChannel(string baseChannel, string serverId)
    {
        return $"{baseChannel}:servers:{serverId}";
    }

    private static async ValueTask UnsubscribeAsync(
        ISubscriber subscriber,
        IEnumerable<RedisChannel> channels,
        Action<RedisChannel, RedisValue> handler)
    {
        foreach (var channel in channels)
        {
            await subscriber.UnsubscribeAsync(channel, handler);
        }
    }

    private sealed class RedisDimSignalSubscription(
        ISubscriber subscriber,
        RedisChannel[] channels,
        Action<RedisChannel, RedisValue> handler) : IDimSignalSubscription
    {
        private readonly ISubscriber _subscriber = subscriber;
        private readonly RedisChannel[] _channels = channels;
        private readonly Action<RedisChannel, RedisValue> _handler = handler;

        public async ValueTask DisposeAsync()
        {
            await UnsubscribeAsync(_subscriber, _channels, _handler);
        }
    }
}
