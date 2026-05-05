using Dim.Abstractions.Routing;
using Dim.Infrastructure.Routing;
using FluentAssertions;

namespace Dim.UnitTests.Routing;

public sealed class LocalConnectionRouteStoreTests
{
    [Fact]
    public void AddShouldAppendNewConnectionToTail()
    {
        var store = new LocalConnectionRouteStore();

        store.Add(CreateRoute("c1"));
        store.Add(CreateRoute("c2"));
        store.Add(CreateRoute("c3"));

        store.Count.Should().Be(3);
        store.GetAll().Select(route => route.ConnectionId).Should().Equal("c1", "c2", "c3");
    }

    [Fact]
    public void MoveToTailShouldRotateRefreshBatch()
    {
        var store = new LocalConnectionRouteStore();
        store.Add(CreateRoute("c1"));
        store.Add(CreateRoute("c2"));
        store.Add(CreateRoute("c3"));

        var batch = store.GetRefreshBatch(2);

        batch.Select(route => route.ConnectionId).Should().Equal("c1", "c2");

        store.MoveToTail(batch);

        store.GetAll().Select(route => route.ConnectionId).Should().Equal("c3", "c1", "c2");
    }

    [Fact]
    public void RemoveShouldDeleteDictionaryEntryAndLinkedListNode()
    {
        var store = new LocalConnectionRouteStore();
        var route = CreateRoute("c2");
        store.Add(CreateRoute("c1"));
        store.Add(route);
        store.Add(CreateRoute("c3"));

        store.Remove(route);

        store.Count.Should().Be(2);
        store.TryGet("c2", out _).Should().BeFalse();
        store.GetAll().Select(item => item.ConnectionId).Should().Equal("c1", "c3");
    }

    [Fact]
    public void AddShouldUpdateExistingConnectionAndMoveItToTail()
    {
        var store = new LocalConnectionRouteStore();
        var updated = CreateRoute("c1", userId: "u2", platform: DimClientPlatform.Android);
        store.Add(CreateRoute("c1"));
        store.Add(CreateRoute("c2"));

        store.Add(updated);

        store.Count.Should().Be(2);
        store.GetAll().Select(route => route.ConnectionId).Should().Equal("c2", "c1");
        store.TryGet("c1", out var current).Should().BeTrue();
        current.Should().Be(updated);
    }

    [Fact]
    public void MoveToTailShouldIgnoreReplacedRouteSnapshot()
    {
        var store = new LocalConnectionRouteStore();
        var oldRoute = CreateRoute("c1");
        var newRoute = CreateRoute("c1", userId: "u2", platform: DimClientPlatform.Android);
        store.Add(oldRoute);
        store.Add(CreateRoute("c2"));

        var batch = store.GetRefreshBatch(1);
        store.Add(newRoute);
        store.MoveToTail(batch);

        store.GetAll().Select(route => route.ConnectionId).Should().Equal("c2", "c1");
        store.TryGet("c1", out var current).Should().BeTrue();
        current.Should().Be(newRoute);
    }

    private static DimConectionRoute CreateRoute(
        string connectionId,
        string userId = "u1",
        DimClientPlatform platform = DimClientPlatform.Web)
    {
        return new DimConectionRoute(
            userId,
            platform,
            connectionId,
            "s1",
            DateTimeOffset.UtcNow);
    }
}
