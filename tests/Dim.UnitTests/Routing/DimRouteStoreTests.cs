using Dim.Infrastructure.Routing;
using FluentAssertions;

namespace Dim.UnitTests.Routing;

public sealed class DimRouteStoreTests
{
    [Fact]
    public void BuildRouteKeyShouldIncludeAppIdAndUserIdInHashTag()
    {
        var key = DimRouteStore.BuildRouteKey("dim:routes:", 1001, "u1");

        key.Should().Be("dim:routes:{1001:u1}");
    }
}
