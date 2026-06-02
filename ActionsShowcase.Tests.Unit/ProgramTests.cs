using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;

namespace ActionsShowcase.Tests.Unit;

public class ProgramTests
{
    [Fact]
    public async Task Development_OpenApi_IsMapped()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseEnvironment("Development"));
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Production_OpenApi_IsNotMapped()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseEnvironment("Production"));
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SecretEndpoint_ReturnsConfiguredSecrets()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/Secret");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadAsStringAsync();
        var secrets = JsonSerializer.Deserialize<string[]>(payload);

        Assert.NotNull(secrets);
        Assert.NotEmpty(secrets!);
    }

    [Fact]
    public async Task RandomEndpoint_ReturnsRandomStrings()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/Random");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadAsStringAsync();
        var values = JsonSerializer.Deserialize<string[]>(payload);

        Assert.NotNull(values);
        Assert.InRange(values!.Length, 2, 10);
        Assert.All(values, s => Assert.InRange(s.Length, 2, 10));
    }
}
