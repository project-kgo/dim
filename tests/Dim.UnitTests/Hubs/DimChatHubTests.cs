using System.Security.Claims;
using Dim.Abstractions.Configuration;
using Dim.Abstractions.Routing;
using Dim.Application.Routing;
using Dim.AspNetCore.Authentication;
using Dim.AspNetCore.Hubs;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Dim.UnitTests.Hubs;

public sealed class DimChatHubTests
{
    [Fact]
    public async Task OnConnectedAsyncWhenAuthenticatorRejectsShouldAbortConnection()
    {
        var store = new InMemoryRouteStore();
        var context = new TestHubCallerContext("c1");
        var hub = CreateHub(new TestAuthenticator(null), store, context, new RecordingHubClients());

        await hub.OnConnectedAsync();

        context.Aborted.Should().BeTrue();
        store.Routes.Should().BeEmpty();
    }

    [Fact]
    public async Task OnConnectedAsyncWhenRouteIsReplacedShouldNotifyOldConnection()
    {
        var store = new InMemoryRouteStore();
        var service = new DimChatRouteService(store);
        await service.ConnectAsync("u1", DimClientPlatform.Web, "old", new DimChatConnectionOptions(), CancellationToken.None);
        var clients = new RecordingHubClients();
        var hub = CreateHub(
            new TestAuthenticator(new DimChatAuthenticationResult("u1", DimClientPlatform.Web)),
            store,
            new TestHubCallerContext("new"),
            clients);

        await hub.OnConnectedAsync();

        var message = clients.Messages.Should().ContainSingle().Subject;
        message.ConnectionId.Should().Be("old");
        message.Method.Should().Be("ForceOffline");
        message.Arguments.Should().ContainSingle().Which.Should().Be("replaced");
    }

    private static DimChatHub CreateHub(
        IDimAuthenticator authenticator,
        InMemoryRouteStore store,
        HubCallerContext context,
        IHubCallerClients clients)
    {
        return new DimChatHub(
            authenticator,
            new DimChatRouteService(store),
            Options.Create(new DimChatOptions()))
        {
            Context = context,
            Clients = clients
        };
    }

    private sealed class TestAuthenticator : IDimAuthenticator
    {
        private readonly DimChatAuthenticationResult? _result;

        public TestAuthenticator(DimChatAuthenticationResult? result)
        {
            _result = result;
        }

        public ValueTask<DimChatAuthenticationResult?> AuthenticateAsync(
            DimAuthenticationContext context,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(_result);
        }
    }

    private sealed class TestHubCallerContext : HubCallerContext
    {
        public TestHubCallerContext(string connectionId)
        {
            ConnectionId = connectionId;
        }

        public bool Aborted { get; private set; }

        public override string ConnectionId { get; }

        public override string? UserIdentifier => null;

        public override ClaimsPrincipal? User => null;

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

    private sealed class InMemoryRouteStore : IDimChatRouteStore
    {
        private readonly Dictionary<string, DimChatRoute> _routes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DimChatRouteScope> _connections = new(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, DimChatRoute> Routes => _routes;

        public ValueTask<DimChatRoute?> GetRouteAsync(DimChatRouteScope scope, CancellationToken cancellationToken)
        {
            _routes.TryGetValue(scope.Key, out var route);
            return ValueTask.FromResult(route);
        }

        public ValueTask SetRouteAsync(DimChatRoute route, TimeSpan ttl, CancellationToken cancellationToken)
        {
            _routes[route.Scope.Key] = route;
            _connections[route.ConnectionId] = route.Scope;
            return ValueTask.CompletedTask;
        }

        public ValueTask<DimChatRouteScope?> GetRouteScopeAsync(string connectionId, CancellationToken cancellationToken)
        {
            _connections.TryGetValue(connectionId, out var scope);
            return ValueTask.FromResult(scope);
        }

        public ValueTask<bool> RemoveRouteIfCurrentAsync(DimChatRouteScope scope, string connectionId, CancellationToken cancellationToken)
        {
            if (!_routes.TryGetValue(scope.Key, out var route) || route.ConnectionId != connectionId)
            {
                return ValueTask.FromResult(false);
            }

            _routes.Remove(scope.Key);
            _connections.Remove(connectionId);
            return ValueTask.FromResult(true);
        }

        public ValueTask<bool> RefreshRouteAsync(DimChatRouteScope scope, string connectionId, TimeSpan ttl, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(
                _routes.TryGetValue(scope.Key, out var route) && route.ConnectionId == connectionId);
        }
    }
}
