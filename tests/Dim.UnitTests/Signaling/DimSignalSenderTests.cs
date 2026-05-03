using Dim.Abstractions.Routing;
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
        var routeStore = new RecordingRouteStore(
        [
            new DimUserConnectionRoute("u1", DimClientPlatform.Web, "c1", "s1"),
            new DimUserConnectionRoute("u2", DimClientPlatform.Ios, "c2", "s2"),
            new DimUserConnectionRoute("u2", DimClientPlatform.Web, "c3", "s2")
        ]);
        var sender = new DimSignalSender(bus, routeStore);
        byte[] payload = [1, 2, 3];

        await sender.SendToUsersAsync(["u1", "u2", "u1"], "chat.message", payload);

        bus.GlobalMessages.Should().BeEmpty();
        bus.ServerMessages.Should().HaveCount(2);

        var s1 = bus.ServerMessages.Should().ContainSingle(message => message.ServerId == "s1").Subject.Message;
        var s1Message = SignalMessage.Parser.ParseFrom(s1);
        s1Message.Target.TargetCase.Should().Be(SignalTarget.TargetOneofCase.Connections);
        s1Message.Target.Connections.ConnectionIds.Should().Equal("c1");
        s1Message.Envelope.SignalType.Should().Be("chat.message");
        s1Message.Envelope.Payload.ToByteArray().Should().Equal(payload);

        var s2 = bus.ServerMessages.Should().ContainSingle(message => message.ServerId == "s2").Subject.Message;
        var s2Message = SignalMessage.Parser.ParseFrom(s2);
        s2Message.Target.Connections.ConnectionIds.Should().Equal("c2", "c3");
        s2Message.Envelope.MessageId.Should().Be(s1Message.Envelope.MessageId);
        s2Message.Envelope.SentAt.Should().Be(s1Message.Envelope.SentAt);
    }

    [Fact]
    public async Task SendToConnectionAsyncShouldPublishToServer()
    {
        var bus = new RecordingSignalBus();
        var sender = new DimSignalSender(bus, new RecordingRouteStore([]));

        byte[] payload = [1, 2];

        await sender.SendToConnectionAsync("s1", "c1", "chat.message", payload);

        var message = bus.ServerMessages.Should().ContainSingle().Subject;
        message.ServerId.Should().Be("s1");

        var envelope = SignalMessage.Parser.ParseFrom(message.Message);
        envelope.Target.Connections.ConnectionIds.Should().Equal("c1");
        envelope.Envelope.Payload.ToByteArray().Should().Equal([1, 2]);
    }

    [Fact]
    public async Task BroadcastAsyncShouldPublishAllSignalEnvelope()
    {
        var bus = new RecordingSignalBus();
        var sender = new DimSignalSender(bus, new RecordingRouteStore([]));

        await sender.BroadcastAsync("system.notice", ReadOnlyMemory<byte>.Empty);

        bus.ServerMessages.Should().BeEmpty();
        var envelope = SignalMessage.Parser.ParseFrom(bus.GlobalMessages.Should().ContainSingle().Subject);
        envelope.Target.TargetCase.Should().Be(SignalTarget.TargetOneofCase.All);
        envelope.Target.All.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsyncWhenRequiredArgumentsAreBlankShouldThrow()
    {
        var sender = new DimSignalSender(new RecordingSignalBus(), new RecordingRouteStore([]));

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await sender.SendToUsersAsync([], "chat.message", ReadOnlyMemory<byte>.Empty));

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await sender.SendToUsersAsync(["u1", ""], "chat.message", ReadOnlyMemory<byte>.Empty));

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await sender.SendToConnectionAsync("", "c1", "chat.message", ReadOnlyMemory<byte>.Empty));

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await sender.SendToConnectionAsync("s1", "", "chat.message", ReadOnlyMemory<byte>.Empty));

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await sender.BroadcastAsync("", ReadOnlyMemory<byte>.Empty));
    }

    private sealed class RecordingSignalBus : IDimSignalBus
    {
        public List<byte[]> GlobalMessages { get; } = [];

        public List<RecordedServerMessage> ServerMessages { get; } = [];

        public ValueTask PublishAsync(
            ReadOnlyMemory<byte> message,
            CancellationToken cancellationToken)
        {
            GlobalMessages.Add(message.ToArray());
            return ValueTask.CompletedTask;
        }

        public ValueTask PublishToServerAsync(
            string serverId,
            ReadOnlyMemory<byte> message,
            CancellationToken cancellationToken)
        {
            ServerMessages.Add(new RecordedServerMessage(serverId, message.ToArray()));
            return ValueTask.CompletedTask;
        }

        public ValueTask<IDimSignalSubscription> SubscribeAsync(
            Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> handler,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public sealed record RecordedServerMessage(
            string ServerId,
            byte[] Message);
    }

    private sealed class RecordingRouteStore(
        IReadOnlyCollection<DimUserConnectionRoute> routes) : IDimChatRouteStore
    {
        public ValueTask<DimReplacedConnectionRoute[]?> SetRouteAsync(
            DimConectionRoute route,
            TimeSpan ttl,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public ValueTask<IReadOnlyCollection<DimUserConnectionRoute>> GetRoutesAsync(
            IReadOnlyCollection<string> userIds,
            CancellationToken cancellationToken)
        {
            var values = routes
                .Where(route => userIds.Contains(route.UserId, StringComparer.Ordinal))
                .ToArray();

            return ValueTask.FromResult<IReadOnlyCollection<DimUserConnectionRoute>>(values);
        }

        public ValueTask<bool> RemoveRouteAsync(
            DimConectionRoute route,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public ValueTask<bool> RefreshRouteAsync(
            DimConectionRoute route,
            TimeSpan ttl,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task RefreshTTLRoutesAsync(
            IEnumerable<DimConectionRoute> routes,
            TimeSpan ttl,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
