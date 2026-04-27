using Dim.Abstractions.Configuration;
using Dim.Abstractions.Routing;
using Dim.Application.Routing;
using FluentAssertions;

namespace Dim.UnitTests.Routing;

public sealed class DimChatRouteServiceTests
{
    [Fact]
    public async Task ConnectAsyncWhenMultiDeviceEnabledShouldKeepDifferentPlatforms()
    {
        var store = new InMemoryRouteStore();
        var service = new DimChatRouteService(store);
        var options = new DimChatConnectionOptions();

        await service.ConnectAsync("u1", DimClientPlatform.Ios, "c1", options, CancellationToken.None);
        await service.ConnectAsync("u1", DimClientPlatform.Web, "c2", options, CancellationToken.None);

        store.Routes.Should().ContainKey("user:u1:platform:ios");
        store.Routes.Should().ContainKey("user:u1:platform:web");
    }

    [Fact]
    public async Task ConnectAsyncWhenSamePlatformConnectedShouldReturnOldConnection()
    {
        var store = new InMemoryRouteStore();
        var service = new DimChatRouteService(store);
        var options = new DimChatConnectionOptions();

        await service.ConnectAsync("u1", DimClientPlatform.Android, "old", options, CancellationToken.None);
        var result = await service.ConnectAsync("u1", DimClientPlatform.Android, "new", options, CancellationToken.None);

        result.ReplacedRoute?.ConnectionId.Should().Be("old");
        store.Routes["user:u1:platform:android"].ConnectionId.Should().Be("new");
    }

    [Fact]
    public async Task ConnectAsyncWhenMultiDeviceDisabledShouldUseUserScope()
    {
        var store = new InMemoryRouteStore();
        var service = new DimChatRouteService(store);
        var options = new DimChatConnectionOptions
        {
            AllowMultiDeviceLogin = false
        };

        await service.ConnectAsync("u1", DimClientPlatform.Ios, "ios-1", options, CancellationToken.None);
        var result = await service.ConnectAsync("u1", DimClientPlatform.Web, "web-1", options, CancellationToken.None);

        result.ReplacedRoute?.ConnectionId.Should().Be("ios-1");
        store.Routes.Should().ContainSingle();
        store.Routes["user:u1"].Platform.Should().Be(DimClientPlatform.Web);
    }

    [Fact]
    public async Task DisconnectAsyncWhenRouteWasReplacedShouldNotRemoveNewRoute()
    {
        var store = new InMemoryRouteStore();
        var service = new DimChatRouteService(store);
        var options = new DimChatConnectionOptions();

        await service.ConnectAsync("u1", DimClientPlatform.Web, "old", options, CancellationToken.None);
        await service.ConnectAsync("u1", DimClientPlatform.Web, "new", options, CancellationToken.None);

        await service.DisconnectAsync("old", CancellationToken.None);

        store.Routes["user:u1:platform:web"].ConnectionId.Should().Be("new");
    }

    [Fact]
    public async Task RefreshRouteAsyncWhenConnectionIsCurrentShouldRefreshRoute()
    {
        var store = new InMemoryRouteStore();
        var service = new DimChatRouteService(store);
        var options = new DimChatConnectionOptions();

        await service.ConnectAsync("u1", DimClientPlatform.Web, "c1", options, CancellationToken.None);
        var refreshed = await service.RefreshRouteAsync("c1", options.RouteTtl, CancellationToken.None);

        refreshed.Should().BeTrue();
        store.RefreshCount.Should().Be(1);
    }

    private sealed class InMemoryRouteStore : IDimChatRouteStore
    {
        private readonly Dictionary<string, DimChatRoute> _routes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DimChatRouteScope> _connections = new(StringComparer.Ordinal);

        public Dictionary<string, DimChatRoute> Routes => _routes;

        public int RefreshCount { get; private set; }

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
            if (!_routes.TryGetValue(scope.Key, out var route) || route.ConnectionId != connectionId)
            {
                return ValueTask.FromResult(false);
            }

            RefreshCount++;
            return ValueTask.FromResult(true);
        }
    }
}
