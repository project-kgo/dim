using Dim.Application.Runtime;
using FluentAssertions;

namespace Dim.UnitTests.Runtime;

public sealed class DimChatRuntimeTests
{
    [Fact]
    public void GetStatusShouldReturnReadyStatus()
    {
        var runtime = new DimChatRuntime();

        var status = runtime.GetStatus();

        status.Name.Should().Be("Dim");
        status.State.Should().Be("Ready");
    }
}
