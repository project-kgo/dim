using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json.Nodes;

namespace Dim.IntegrationTests.Hosting;

public sealed class DimSampleHostTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task HealthEndpointShouldReturnOk()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/dim/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HubNegotiateShouldOnlyExposeWebSocketsTransport()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            "/dim/hub/negotiate?negotiateVersion=1&access_token=integration-test",
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        var transports = payload?["availableTransports"]?.AsArray();

        transports.Should().NotBeNull();
        transports!.Should().ContainSingle();
        transports[0]?["transport"]?.GetValue<string>().Should().Be("WebSockets");
    }
}
