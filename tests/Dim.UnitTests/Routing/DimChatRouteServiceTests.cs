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
        var store = new TestDimChatRouteStore();
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
        var store = new TestDimChatRouteStore();
        var service = new DimChatRouteService(store);
        var options = new DimChatConnectionOptions();

        await service.ConnectAsync("u1", DimClientPlatform.Android, "old", options, CancellationToken.None);
        var result = await service.ConnectAsync("u1", DimClientPlatform.Android, "new", options, CancellationToken.None);

        result.PreviousConnectionIds.Should().Equal("old");
        store.Routes["user:u1:platform:android"].ConnectionId.Should().Be("new");
    }

    [Fact]
    public async Task ConnectAsyncWhenMultiDeviceDisabledShouldUseUserScope()
    {
        var store = new TestDimChatRouteStore(allowMultiDeviceLogin: false);
        var service = new DimChatRouteService(store);
        var options = new DimChatConnectionOptions
        {
            AllowMultiDeviceLogin = false
        };

        await service.ConnectAsync("u1", DimClientPlatform.Ios, "ios-1", options, CancellationToken.None);
        var result = await service.ConnectAsync("u1", DimClientPlatform.Web, "web-1", options, CancellationToken.None);

        result.PreviousConnectionIds.Should().Equal("ios-1");
        store.Routes.Should().ContainSingle();
        store.Routes["user:u1"].Platform.Should().Be(DimClientPlatform.Web);
    }

    [Fact]
    public async Task DisconnectAsyncWhenRouteWasReplacedShouldNotRemoveNewRoute()
    {
        var store = new TestDimChatRouteStore();
        var service = new DimChatRouteService(store);
        var options = new DimChatConnectionOptions();

        var oldRoute = (await service.ConnectAsync("u1", DimClientPlatform.Web, "old", options, CancellationToken.None)).CurrentRoute;
        await service.ConnectAsync("u1", DimClientPlatform.Web, "new", options, CancellationToken.None);

        await service.DisconnectAsync(oldRoute, CancellationToken.None);

        store.Routes["user:u1:platform:web"].ConnectionId.Should().Be("new");
    }

    [Fact]
    public async Task RefreshRouteAsyncWhenConnectionIsCurrentShouldRefreshRoute()
    {
        var store = new TestDimChatRouteStore();
        var service = new DimChatRouteService(store);
        var options = new DimChatConnectionOptions();

        var route = (await service.ConnectAsync("u1", DimClientPlatform.Web, "c1", options, CancellationToken.None)).CurrentRoute;
        var refreshed = await service.RefreshRouteAsync(route, options.RouteTtl, CancellationToken.None);

        refreshed.Should().BeTrue();
        store.RefreshCount.Should().Be(1);
    }
}
