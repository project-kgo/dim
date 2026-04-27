using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace Dim.IntegrationTests.Hosting;

public sealed class DimSampleHostTests
{
    [Fact]
    public async Task HealthEndpointShouldReturnOk()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/dim/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
