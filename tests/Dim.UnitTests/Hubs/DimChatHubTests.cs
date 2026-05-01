using System.Security.Claims;
using Dim.Abstractions.Authentication;
using Dim.Abstractions.Configuration;
using Dim.Abstractions.Routing;
using Dim.Application.Routing;
using Dim.AspNetCore.Hubs;
using Dim.UnitTests.Routing;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Dim.UnitTests.Hubs;

public sealed class DimChatHubTests
{
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
        var service = new DimChatRouteService(store);
        await service.ConnectAsync("u1", DimClientPlatform.Web, "old", new DimChatConnectionOptions(), CancellationToken.None);
        var clients = new RecordingHubClients();
        var hub = CreateHub(
            store,
            new TestHubCallerContext("new", CreateUser("u1", DimClientPlatform.Web)),
            clients);

        await hub.OnConnectedAsync();

        var message = clients.Messages.Should().ContainSingle().Subject;
        message.ConnectionId.Should().Be("old");
        message.Method.Should().Be("ForceOffline");
        message.Arguments.Should().ContainSingle().Which.Should().Be("replaced");
    }

    private static DimChatHub CreateHub(
        TestDimChatRouteStore store,
        HubCallerContext context,
        IHubCallerClients clients)
    {
        return new DimChatHub(
            new DimChatRouteService(store),
            Options.Create(new DimChatOptions()))
        {
            Context = context,
            Clients = clients
        };
    }

    private static ClaimsPrincipal CreateUser(
        string userId,
        DimClientPlatform platform)
    {
        var claims = new[]
        {
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
}
