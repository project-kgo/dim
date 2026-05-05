using Dim.Abstractions.Configuration;
using FluentAssertions;

namespace Dim.UnitTests.Configuration;

public sealed class DimChatOptionsTests
{
    [Fact]
    public void DefaultOptionsShouldExposeExpectedRoutes()
    {
        var options = new DimChatOptions();

        options.EndpointPrefix.Should().Be("/dim");
        options.HubPath.Should().Be("/hub");
        options.Connection.AllowMultiDeviceLogin.Should().BeTrue();
        options.Connection.RouteKeyPrefix.Should().Be("dim:routes");
        options.Connection.RouteTtl.Should().Be(TimeSpan.FromDays(7));
        options.Storage.RedisStreamName.Should().Be("dim:messages");
        options.Storage.CapStorageSchema.Should().Be("cap");
        options.Storage.CapDefaultGroupName.Should().Be("dim");
        options.Signaling.RedisChannel.Should().Be("dim:signals");
        options.Signaling.ClientMethodName.Should().Be("ReceiveSignal");
    }
}
