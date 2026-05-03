using Dim.Abstractions.Configuration;
using Dim.Application.Signaling;
using Dim.AspNetCore.Hubs;
using Dim.AspNetCore.Signaling;
using Dim.Contracts;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Dim.UnitTests.Signaling;

public sealed class DimSignalHubDispatcherTests
{
    [Fact]
    public async Task DispatchAsyncShouldSendUsersSignalToUserClients()
    {
        var clients = new RecordingHubClients();
        var dispatcher = CreateDispatcher(clients);
        var signalMessage = CreateMessage(new SignalTarget
        {
            Users = new SignalUserTarget
            {
                UserIds = { "u1", "u2" }
            }
        });

        await dispatcher.DispatchAsync(signalMessage, CancellationToken.None);

        var message = clients.Messages.Should().ContainSingle().Subject;
        message.Target.Should().Be("users:u1,u2");
        message.Method.Should().Be("ReceiveSignal");
        SignalEnvelope.Parser.ParseFrom(message.GetPayload()).SignalType.Should().Be("chat.message");
    }

    [Fact]
    public async Task DispatchAsyncShouldBroadcastAllSignal()
    {
        var clients = new RecordingHubClients();
        var dispatcher = CreateDispatcher(clients);
        var signalMessage = CreateMessage(new SignalTarget { All = true });

        await dispatcher.DispatchAsync(signalMessage, CancellationToken.None);

        var message = clients.Messages.Should().ContainSingle().Subject;
        message.Target.Should().Be("all");
        SignalEnvelope.Parser.ParseFrom(message.GetPayload()).SignalType.Should().Be("chat.message");
    }

    [Fact]
    public async Task DispatchAsyncShouldSendSignalToConnections()
    {
        var clients = new RecordingHubClients();
        var dispatcher = CreateDispatcher(clients);
        var signalMessage = CreateMessage(new SignalTarget
        {
            Connections = new SignalConnectionTarget
            {
                ConnectionIds = { "c1", "c2" }
            }
        });

        await dispatcher.DispatchAsync(signalMessage, CancellationToken.None);

        var message = clients.Messages.Should().ContainSingle().Subject;
        message.Target.Should().Be("connections:c1,c2");
        message.Method.Should().Be("ReceiveSignal");
        SignalEnvelope.Parser.ParseFrom(message.GetPayload()).SignalType.Should().Be("chat.message");
    }

    [Fact]
    public async Task DispatchAsyncShouldForceOfflineConnections()
    {
        var clients = new RecordingHubClients();
        var dispatcher = CreateDispatcher(clients);
        var signalMessage = CreateMessage(new SignalTarget
        {
            Connections = new SignalConnectionTarget
            {
                ConnectionIds = { "c1" }
            }
        });
        signalMessage.Envelope.SignalType = DimInternalSignalTypes.ForceOffline;
        signalMessage.Envelope.Payload = ByteString.CopyFrom("replaced"u8.ToArray());

        await dispatcher.DispatchAsync(signalMessage, CancellationToken.None);

        var message = clients.Messages.Should().ContainSingle().Subject;
        message.Target.Should().Be("connections:c1");
        message.Method.Should().Be("ForceOffline");
        message.Arguments.Should().ContainSingle().Which.Should().Be("replaced");
    }

    private static DimSignalHubDispatcher CreateDispatcher(RecordingHubClients clients)
    {
        return new DimSignalHubDispatcher(
            new TestHubContext(clients),
            Options.Create(new DimChatOptions()));
    }

    private static SignalMessage CreateMessage(SignalTarget target)
    {
        return new SignalMessage
        {
            Target = target,
            Envelope = new SignalEnvelope
            {
                MessageId = "m1",
                SignalType = "chat.message",
                Payload = ByteString.CopyFrom([1, 2, 3]),
                SentAt = 1
            }
        };
    }

    private sealed class TestHubContext(RecordingHubClients clients) : IHubContext<DimChatHub>
    {
        public IHubClients Clients { get; } = clients;

        public IGroupManager Groups { get; } = new NoopGroupManager();
    }

    private sealed class RecordingHubClients : IHubClients
    {
        public List<RecordedClientMessage> Messages { get; } = [];

        public IClientProxy All => new RecordingClientProxy("all", Messages);

        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds)
        {
            return new RecordingClientProxy("all", Messages);
        }

        public IClientProxy Client(string connectionId)
        {
            return new RecordingClientProxy($"connection:{connectionId}", Messages);
        }

        public IClientProxy Clients(IReadOnlyList<string> connectionIds)
        {
            return new RecordingClientProxy($"connections:{string.Join(',', connectionIds)}", Messages);
        }

        public IClientProxy Group(string groupName)
        {
            return new RecordingClientProxy($"group:{groupName}", Messages);
        }

        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds)
        {
            return new RecordingClientProxy($"group:{groupName}", Messages);
        }

        public IClientProxy Groups(IReadOnlyList<string> groupNames)
        {
            return new RecordingClientProxy($"groups:{string.Join(',', groupNames)}", Messages);
        }

        public IClientProxy User(string userId)
        {
            return new RecordingClientProxy($"user:{userId}", Messages);
        }

        public IClientProxy Users(IReadOnlyList<string> userIds)
        {
            return new RecordingClientProxy($"users:{string.Join(',', userIds)}", Messages);
        }
    }

    private sealed class RecordingClientProxy(
        string target,
        List<RecordedClientMessage> messages) : IClientProxy
    {
        public Task SendCoreAsync(
            string method,
            object?[] args,
            CancellationToken cancellationToken = default)
        {
            messages.Add(new RecordedClientMessage(target, method, args));
            return Task.CompletedTask;
        }
    }

    private sealed class NoopGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed record RecordedClientMessage(
        string Target,
        string Method,
        object?[] Arguments)
    {
        public byte[] GetPayload()
        {
            return Arguments.Should().ContainSingle().Subject.Should().BeOfType<byte[]>().Subject;
        }
    }
}
