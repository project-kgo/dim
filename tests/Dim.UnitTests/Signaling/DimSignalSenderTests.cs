using Dim.Application.Signaling;
using Dim.Contracts;
using FluentAssertions;

namespace Dim.UnitTests.Signaling;

public sealed class DimSignalSenderTests
{
    [Fact]
    public async Task SendToUsersAsyncShouldPublishUsersSignalEnvelope()
    {
        var bus = new RecordingSignalBus();
        var sender = new DimSignalSender(bus);
        byte[] payload = [1, 2, 3];

        await sender.SendToUsersAsync(["u1", "u2", "u1"], "chat.message", payload);

        var envelope = bus.GetSingleEnvelope();
        envelope.SignalType.Should().Be("chat.message");
        envelope.Target.TargetCase.Should().Be(SignalTarget.TargetOneofCase.Users);
        envelope.Target.Users.UserIds.Should().Equal("u1", "u2");
        envelope.Payload.ToByteArray().Should().Equal(payload);
        envelope.MessageId.Should().NotBeNullOrWhiteSpace();
        envelope.SentAtUnixTimeMilliseconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SendToConnectionAsyncShouldPublishConnectionSignalEnvelope()
    {
        var bus = new RecordingSignalBus();
        var sender = new DimSignalSender(bus);

        await sender.SendToConnectionAsync("c1", "typing", ReadOnlyMemory<byte>.Empty);

        var envelope = bus.GetSingleEnvelope();
        envelope.Target.TargetCase.Should().Be(SignalTarget.TargetOneofCase.ConnectionId);
        envelope.Target.ConnectionId.Should().Be("c1");
    }

    [Fact]
    public async Task BroadcastAsyncShouldPublishAllSignalEnvelope()
    {
        var bus = new RecordingSignalBus();
        var sender = new DimSignalSender(bus);

        await sender.BroadcastAsync("system.notice", ReadOnlyMemory<byte>.Empty);

        var envelope = bus.GetSingleEnvelope();
        envelope.Target.TargetCase.Should().Be(SignalTarget.TargetOneofCase.All);
        envelope.Target.All.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsyncWhenRequiredArgumentsAreBlankShouldThrow()
    {
        var sender = new DimSignalSender(new RecordingSignalBus());

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await sender.SendToUsersAsync([], "chat.message", ReadOnlyMemory<byte>.Empty));

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await sender.SendToUsersAsync(["u1", ""], "chat.message", ReadOnlyMemory<byte>.Empty));

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await sender.SendToConnectionAsync("", "chat.message", ReadOnlyMemory<byte>.Empty));

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await sender.BroadcastAsync("", ReadOnlyMemory<byte>.Empty));
    }

    private sealed class RecordingSignalBus : IDimSignalBus
    {
        private readonly List<byte[]> _messages = [];

        public ValueTask PublishAsync(
            ReadOnlyMemory<byte> message,
            CancellationToken cancellationToken)
        {
            _messages.Add(message.ToArray());
            return ValueTask.CompletedTask;
        }

        public ValueTask<IDimSignalSubscription> SubscribeAsync(
            Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> handler,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public SignalEnvelope GetSingleEnvelope()
        {
            return SignalEnvelope.Parser.ParseFrom(_messages.Should().ContainSingle().Subject);
        }
    }
}
