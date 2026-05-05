using Dim.Abstractions.Configuration;
using Dim.Abstractions.Routing;
using Dim.Application.Routing;
using FluentAssertions;

namespace Dim.UnitTests.Routing;

public sealed class DimChatRouteServiceTests
{
    private const long AppId = 1001;

    [Fact]
    public async Task ConnectAsyncWhenMultiDeviceEnabledShouldKeepDifferentPlatforms()
    {
        var store = new TestDimChatRouteStore();
        var service = CreateService(store);
        var options = new DimChatConnectionOptions();

        await service.ConnectAsync(AppId, "u1", DimClientPlatform.Ios, "c1", options, CancellationToken.None);
        await service.ConnectAsync(AppId, "u1", DimClientPlatform.Web, "c2", options, CancellationToken.None);

        store.Routes.Should().ContainKey("app:1001:user:u1:platform:ios");
        store.Routes.Should().ContainKey("app:1001:user:u1:platform:web");
    }

    [Fact]
    public async Task ConnectAsyncWhenAppIdIsInvalidShouldThrow()
    {
        var service = CreateService(new TestDimChatRouteStore());
        var options = new DimChatConnectionOptions();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await service.ConnectAsync(0, "u1", DimClientPlatform.Web, "c1", options, CancellationToken.None));
    }

    [Fact]
    public async Task ConnectAsyncWhenSamePlatformConnectedShouldReturnOldConnection()
    {
        var store = new TestDimChatRouteStore();
        var service = CreateService(store);
        var options = new DimChatConnectionOptions();

        await service.ConnectAsync(AppId, "u1", DimClientPlatform.Android, "old", options, CancellationToken.None);
        var result = await service.ConnectAsync(AppId, "u1", DimClientPlatform.Android, "new", options, CancellationToken.None);

        var replacedRoute = result.ReplacedRoutes.Should().ContainSingle().Subject;
        replacedRoute.AppId.Should().Be(AppId);
        replacedRoute.ConnectionId.Should().Be("old");
        replacedRoute.ServerId.Should().NotBeNullOrWhiteSpace();
        store.Routes["app:1001:user:u1:platform:android"].ConnectionId.Should().Be("new");
    }

    [Fact]
    public async Task ConnectAsyncWhenSameUserInDifferentAppsShouldKeepRoutesIsolated()
    {
        var store = new TestDimChatRouteStore();
        var service = CreateService(store);
        var options = new DimChatConnectionOptions();

        await service.ConnectAsync(AppId, "u1", DimClientPlatform.Web, "app-1", options, CancellationToken.None);
        var result = await service.ConnectAsync(2002, "u1", DimClientPlatform.Web, "app-2", options, CancellationToken.None);

        result.ReplacedRoutes.Should().BeNull();
        store.Routes["app:1001:user:u1:platform:web"].ConnectionId.Should().Be("app-1");
        store.Routes["app:2002:user:u1:platform:web"].ConnectionId.Should().Be("app-2");
    }

    [Fact]
    public async Task ConnectAsyncShouldUseStableServerId()
    {
        var store = new TestDimChatRouteStore();
        var service = CreateService(store);
        var options = new DimChatConnectionOptions();

        var first = await service.ConnectAsync(AppId, "u1", DimClientPlatform.Ios, "c1", options, CancellationToken.None);
        var second = await service.ConnectAsync(AppId, "u1", DimClientPlatform.Web, "c2", options, CancellationToken.None);

        first.CurrentRoute.ServerId.Should().NotBeNullOrWhiteSpace();
        second.CurrentRoute.ServerId.Should().Be(first.CurrentRoute.ServerId);
        store.ServerRoutes["app:1001:user:u1:platform:ios"].Should().Be(first.CurrentRoute.ServerId);
        store.ServerRoutes["app:1001:user:u1:platform:web"].Should().Be(first.CurrentRoute.ServerId);
    }

    [Fact]
    public async Task ConnectAsyncWhenMultiDeviceDisabledShouldUseUserScope()
    {
        var store = new TestDimChatRouteStore(allowMultiDeviceLogin: false);
        var service = CreateService(store);
        var options = new DimChatConnectionOptions
        {
            AllowMultiDeviceLogin = false
        };

        await service.ConnectAsync(AppId, "u1", DimClientPlatform.Ios, "ios-1", options, CancellationToken.None);
        var result = await service.ConnectAsync(AppId, "u1", DimClientPlatform.Web, "web-1", options, CancellationToken.None);

        result.ReplacedRoutes.Should()
            .ContainSingle()
            .Which.ConnectionId.Should().Be("ios-1");
        store.Routes.Should().ContainSingle();
        store.Routes["app:1001:user:u1"].Platform.Should().Be(DimClientPlatform.Web);
    }

    [Fact]
    public async Task ConnectAsyncWhenMultiDeviceDisabledShouldOnlyReplaceSameAppUser()
    {
        var store = new TestDimChatRouteStore(allowMultiDeviceLogin: false);
        var service = CreateService(store);
        var options = new DimChatConnectionOptions
        {
            AllowMultiDeviceLogin = false
        };

        await service.ConnectAsync(AppId, "u1", DimClientPlatform.Ios, "app-1", options, CancellationToken.None);
        var result = await service.ConnectAsync(2002, "u1", DimClientPlatform.Web, "app-2", options, CancellationToken.None);

        result.ReplacedRoutes.Should().BeNull();
        store.Routes["app:1001:user:u1"].ConnectionId.Should().Be("app-1");
        store.Routes["app:2002:user:u1"].ConnectionId.Should().Be("app-2");
    }

    [Fact]
    public async Task DisconnectAsyncWhenRouteWasReplacedShouldNotRemoveNewRoute()
    {
        var store = new TestDimChatRouteStore();
        var service = CreateService(store);
        var options = new DimChatConnectionOptions();

        var oldRoute = (await service.ConnectAsync(AppId, "u1", DimClientPlatform.Web, "old", options, CancellationToken.None)).CurrentRoute;
        await service.ConnectAsync(AppId, "u1", DimClientPlatform.Web, "new", options, CancellationToken.None);

        await service.DisconnectAsync(oldRoute, CancellationToken.None);

        store.Routes["app:1001:user:u1:platform:web"].ConnectionId.Should().Be("new");
    }

    [Fact]
    public async Task RefreshRouteAsyncWhenConnectionIsCurrentShouldRefreshRoute()
    {
        var store = new TestDimChatRouteStore();
        var service = CreateService(store);
        var options = new DimChatConnectionOptions();

        var route = (await service.ConnectAsync(AppId, "u1", DimClientPlatform.Web, "c1", options, CancellationToken.None)).CurrentRoute;
        var refreshed = await service.RefreshRouteAsync(route, options.RouteTtl, CancellationToken.None);

        refreshed.Should().BeTrue();
        store.RefreshCount.Should().Be(1);
    }

    private static DimChatRouteService CreateService(TestDimChatRouteStore store)
    {
        return new DimChatRouteService(store, new TestLocalConnectionRouteStore(), new DimServerIdentity());
    }
}
