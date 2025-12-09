using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Lucca.Infra.Haproxy.AgentCheck.Tests;

public class HttpEndpointTests(ITestOutputHelper outputHelper) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetRoot_ReturnsJsonWithStateInfo()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("isUp", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("weight", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MaintenanceEndpoint_WithoutAuth_ReturnsUnauthorized()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/admin/maintenance", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReadyEndpoint_WithoutAuth_ReturnsUnauthorized()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/admin/ready", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MaintenanceEndpoint_WithBasicAuth_SetsMaintenanceMode()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CreateBasicAuthHeader("admin", "admin");

        var response = await client.PostAsync("/admin/maintenance", null, TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        // Verify maintenance mode is set
        var maintenanceStatus = factory.Services.GetRequiredService<MaintenanceStatus>();
        Assert.True(maintenanceStatus.IsMaintenance);
    }

    [Fact]
    public async Task ReadyEndpoint_WithBasicAuth_ClearsMaintenanceMode()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CreateBasicAuthHeader("admin", "admin");

        // First set maintenance mode
        var maintenanceStatus = factory.Services.GetRequiredService<MaintenanceStatus>();
        maintenanceStatus.IsMaintenance = true;

        var response = await client.PostAsync("/admin/ready", null, TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        Assert.False(maintenanceStatus.IsMaintenance);
    }

    [Fact]
    public async Task MaintenanceEndpoint_WithWrongCredentials_ReturnsUnauthorized()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CreateBasicAuthHeader("wrong", "credentials");

        var response = await client.PostAsync("/admin/maintenance", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddFakeLogging(o => o.OutputSink = outputHelper.WriteLine);
                });
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Schemes:Basic:Username"] = "admin",
                        ["Authentication:Schemes:Basic:Password"] = "admin"
                    });
                });
            });
    }

    private static AuthenticationHeaderValue CreateBasicAuthHeader(string username, string password)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        return new AuthenticationHeaderValue("Basic", credentials);
    }
}
