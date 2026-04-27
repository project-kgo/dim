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
        options.Storage.RedisStreamName.Should().Be("dim:messages");
    }
}
