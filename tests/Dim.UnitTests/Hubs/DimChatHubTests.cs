using System.Security.Claims;
using Dim.Abstractions.Authentication;
using Dim.Abstractions.Configuration;
using Dim.Abstractions.Routing;
using Dim.Abstractions.Signaling;
using Dim.Application.Routing;
using Dim.Application.Signaling;
using Dim.AspNetCore.Hubs;
using Dim.UnitTests.Routing;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace Dim.UnitTests.Hubs;

public sealed class DimChatHubTests
{
    private const long AppId = 1001;

    [Fact]
    public async Task OnConnectedAsyncWhenUserClaimsInvalidShouldAbortConnection()
    {
        var store = new TestDimChatRouteStore();
        var context = new TestHubCallerContext("c1");
        var hub = CreateHub(store, context, new RecordingHubClients());

        await hub.OnConnectedAsync();

        context.Aborted.Should().BeTrue();
        store.Routes.Should().BeEmpty();
    }

    [Fact]
    public async Task OnConnectedAsyncWhenRouteIsReplacedShouldNotifyOldConnection()
    {
        var store = new TestDimChatRouteStore();
        var service = new DimChatRouteService(store, new TestLocalConnectionRouteStore(), new DimServerIdentity());
        var oldRoute = (await service.ConnectAsync(
            AppId,
            "u1",
            DimClientPlatform.Web,
            "old",
            new DimChatConnectionOptions(),
            CancellationToken.None)).CurrentRoute;
        var clients = new RecordingHubClients();
        var signalSender = new RecordingSignalSender();
        var hub = CreateHub(
            store,
            new TestHubCallerContext("new", CreateUser(AppId, "u1", DimClientPlatform.Web)),
            clients,
            signalSender);

        await hub.OnConnectedAsync();

        clients.Messages.Should().BeEmpty();

        var message = signalSender.ConnectionMessages.Should().ContainSingle().Subject;
        message.AppId.Should().Be(AppId);
        message.ServerId.Should().Be(oldRoute.ServerId);
        message.ConnectionId.Should().Be("old");
        message.SignalType.Should().Be(DimInternalSignalTypes.ForceOffline);
        message.Payload.ToArray().Should().Equal("replaced"u8.ToArray());
    }

    [Fact]
    public async Task OnConnectedAsyncWhenForceOfflinePublishFailsShouldKeepNewConnection()
    {
        var store = new TestDimChatRouteStore();
        var service = new DimChatRouteService(store, new TestLocalConnectionRouteStore(), new DimServerIdentity());
        await service.ConnectAsync(
            AppId,
            "u1",
            DimClientPlatform.Web,
            "old",
            new DimChatConnectionOptions(),
            CancellationToken.None);
        var context = new TestHubCallerContext("new", CreateUser(AppId, "u1", DimClientPlatform.Web));
        var hub = CreateHub(
            store,
            context,
            new RecordingHubClients(),
            new ThrowingSignalSender());

        await hub.OnConnectedAsync();

        context.Aborted.Should().BeFalse();
        store.Routes["app:1001:user:u1:platform:web"].ConnectionId.Should().Be("new");
    }

    [Fact]
    public async Task OnConnectedAsyncShouldAddConnectionToAppGroup()
    {
        var groups = new RecordingGroupManager();
        var hub = CreateHub(
            new TestDimChatRouteStore(),
            new TestHubCallerContext("c1", CreateUser(AppId, "u1", DimClientPlatform.Web)),
            new RecordingHubClients(),
            groupManager: groups);

        await hub.OnConnectedAsync();

        groups.AddedGroups.Should().ContainSingle().Which.Should().Be(("c1", "dim:app:1001"));
    }

    private static DimChatHub CreateHub(
        TestDimChatRouteStore store,
        HubCallerContext context,
        IHubCallerClients clients,
        IDimSignalSender? signalSender = null,
        IGroupManager? groupManager = null)
    {
        return new DimChatHub(
            new DimChatRouteService(store, new TestLocalConnectionRouteStore(), new DimServerIdentity()),
            signalSender ?? new RecordingSignalSender(),
            NullLogger<DimChatHub>.Instance,
            Options.Create(new DimChatOptions()))
        {
            Context = context,
            Clients = clients,
            Groups = groupManager ?? new RecordingGroupManager()
        };
    }

    private static ClaimsPrincipal CreateUser(
        long appId,
        string userId,
        DimClientPlatform platform)
    {
        var claims = new[]
        {
            new Claim(AuthConstants.AppIdClaim, appId.ToString(CultureInfo.InvariantCulture)),
            new Claim(AuthConstants.UserIdClaim, userId),
            new Claim(AuthConstants.PlatformClaim, platform.ToString())
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, AuthConstants.Scheme));
    }

    private sealed class TestHubCallerContext : HubCallerContext
    {
        private readonly ClaimsPrincipal? _user;

        public TestHubCallerContext(
            string connectionId,
            ClaimsPrincipal? user = null)
        {
            ConnectionId = connectionId;
            _user = user;
        }

        public bool Aborted { get; private set; }

        public override string ConnectionId { get; }

        public override string? UserIdentifier => null;

        public override ClaimsPrincipal? User => _user;

        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();

        public override IFeatureCollection Features { get; } = new FeatureCollection();

        public override CancellationToken ConnectionAborted { get; } = CancellationToken.None;

        public override void Abort()
        {
            Aborted = true;
        }
    }

    private sealed class RecordingHubClients : IHubCallerClients
    {
        public List<RecordedClientMessage> Messages { get; } = [];

        public IClientProxy All => new RecordingClientProxy("*", Messages);

        public IClientProxy Caller => new RecordingClientProxy("caller", Messages);

        public IClientProxy Others => new RecordingClientProxy("others", Messages);

        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds)
        {
            return new RecordingClientProxy("*", Messages);
        }

        public IClientProxy Client(string connectionId)
        {
            return new RecordingClientProxy(connectionId, Messages);
        }

        public IClientProxy Clients(IReadOnlyList<string> connectionIds)
        {
            return new RecordingClientProxy(string.Join(',', connectionIds), Messages);
        }

        public IClientProxy Group(string groupName)
        {
            return new RecordingClientProxy(groupName, Messages);
        }

        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds)
        {
            return new RecordingClientProxy(groupName, Messages);
        }

        public IClientProxy Groups(IReadOnlyList<string> groupNames)
        {
            return new RecordingClientProxy(string.Join(',', groupNames), Messages);
        }

        public IClientProxy OthersInGroup(string groupName)
        {
            return new RecordingClientProxy(groupName, Messages);
        }

        public IClientProxy User(string userId)
        {
            return new RecordingClientProxy(userId, Messages);
        }

        public IClientProxy Users(IReadOnlyList<string> userIds)
        {
            return new RecordingClientProxy(string.Join(',', userIds), Messages);
        }
    }

    private sealed class RecordingClientProxy : IClientProxy
    {
        private readonly string _connectionId;
        private readonly List<RecordedClientMessage> _messages;

        public RecordingClientProxy(string connectionId, List<RecordedClientMessage> messages)
        {
            _connectionId = connectionId;
            _messages = messages;
        }

        public Task SendCoreAsync(
            string method,
            object?[] args,
            CancellationToken cancellationToken = default)
        {
            _messages.Add(new RecordedClientMessage(_connectionId, method, args));
            return Task.CompletedTask;
        }
    }

    private sealed record RecordedClientMessage(
        string ConnectionId,
        string Method,
        object?[] Arguments);

    private sealed class RecordingGroupManager : IGroupManager
    {
        public List<(string ConnectionId, string GroupName)> AddedGroups { get; } = [];

        public List<(string ConnectionId, string GroupName)> RemovedGroups { get; } = [];

        public Task AddToGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default)
        {
            AddedGroups.Add((connectionId, groupName));
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default)
        {
            RemovedGroups.Add((connectionId, groupName));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingSignalSender : IDimSignalSender
    {
        public List<RecordedSignalConnectionMessage> ConnectionMessages { get; } = [];

        public ValueTask SendToUsersAsync(
            long appId,
            IReadOnlyCollection<string> userIds,
            string signalType,
            ReadOnlyMemory<byte> payload,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask SendToConnectionAsync(
            long appId,
            string serverId,
            string connectionId,
            string signalType,
            ReadOnlyMemory<byte> payload,
            CancellationToken cancellationToken = default)
        {
            ConnectionMessages.Add(new RecordedSignalConnectionMessage(
                appId,
                serverId,
                connectionId,
                signalType,
                payload.ToArray()));
            return ValueTask.CompletedTask;
        }

        public ValueTask BroadcastAsync(
            long appId,
            string signalType,
            ReadOnlyMemory<byte> payload,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed record RecordedSignalConnectionMessage(
        long AppId,
        string ServerId,
        string ConnectionId,
        string SignalType,
        ReadOnlyMemory<byte> Payload);

    private sealed class ThrowingSignalSender : IDimSignalSender
    {
        public ValueTask SendToUsersAsync(
            long appId,
            IReadOnlyCollection<string> userIds,
            string signalType,
            ReadOnlyMemory<byte> payload,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask SendToConnectionAsync(
            long appId,
            string serverId,
            string connectionId,
            string signalType,
            ReadOnlyMemory<byte> payload,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("publish failed");
        }

        public ValueTask BroadcastAsync(
            long appId,
            string signalType,
            ReadOnlyMemory<byte> payload,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
