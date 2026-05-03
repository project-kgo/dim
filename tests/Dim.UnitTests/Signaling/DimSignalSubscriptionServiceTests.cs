using Dim.Application.Signaling;
using Dim.Contracts;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dim.UnitTests.Signaling;

public sealed class DimSignalSubscriptionServiceTests
{
    [Fact]
    public async Task SubscribedHandlerShouldDispatchValidSignalEnvelope()
    {
        var bus = new CapturingSignalBus();
        var dispatcher = new RecordingLocalSignalDispatcher();
        var service = new DimSignalSubscriptionService(
            bus,
            dispatcher,
            NullLogger<DimSignalSubscriptionService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await bus.Subscribed.Task.WaitAsync(TimeSpan.FromSeconds(1));

        var signalMessage = CreateMessage();
        await bus.Handler!(signalMessage.ToByteArray(), CancellationToken.None);

        dispatcher.Envelopes.Should().ContainSingle()
            .Which.Envelope.MessageId.Should().Be(signalMessage.Envelope.MessageId);

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SubscribedHandlerWhenMessageInvalidShouldSkipWithoutThrowing()
    {
        var bus = new CapturingSignalBus();
        var dispatcher = new RecordingLocalSignalDispatcher();
        var service = new DimSignalSubscriptionService(
            bus,
            dispatcher,
            NullLogger<DimSignalSubscriptionService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await bus.Subscribed.Task.WaitAsync(TimeSpan.FromSeconds(1));

        byte[] invalidMessage = [1, 2, 3];
        var action = async () => await bus.Handler!(invalidMessage, CancellationToken.None);

        await action.Should().NotThrowAsync();
        dispatcher.Envelopes.Should().BeEmpty();

        await service.StopAsync(CancellationToken.None);
    }

    private static SignalMessage CreateMessage()
    {
        return new SignalMessage
        {
            Target = new SignalTarget
            {
                Users = new SignalUserTarget
                {
                    UserIds = { "u1" }
                }
            },
            Envelope = new SignalEnvelope
            {
                MessageId = "m1",
                SignalType = "chat.message",
                Payload = ByteString.CopyFrom([1, 2, 3]),
                SentAt = 1
            }
        };
    }

    private sealed class CapturingSignalBus : IDimSignalBus
    {
        public TaskCompletionSource Subscribed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask>? Handler { get; private set; }

        public ValueTask PublishAsync(
            ReadOnlyMemory<byte> message,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public ValueTask PublishToServerAsync(
            string serverId,
            ReadOnlyMemory<byte> message,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public ValueTask<IDimSignalSubscription> SubscribeAsync(
            Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> handler,
            CancellationToken cancellationToken)
        {
            Handler = handler;
            Subscribed.SetResult();
            return ValueTask.FromResult<IDimSignalSubscription>(new NoopSignalSubscription());
        }
    }

    private sealed class RecordingLocalSignalDispatcher : IDimLocalSignalDispatcher
    {
        public List<SignalMessage> Envelopes { get; } = [];

        public ValueTask DispatchAsync(
            SignalMessage envelope,
            CancellationToken cancellationToken)
        {
            Envelopes.Add(envelope);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class NoopSignalSubscription : IDimSignalSubscription
    {
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
