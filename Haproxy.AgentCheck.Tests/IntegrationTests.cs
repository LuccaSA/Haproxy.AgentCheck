using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Lucca.Infra.Haproxy.AgentCheck.Tests;
#pragma warning disable S1144
#pragma warning disable S125
public class IntegrationTests(ITestOutputHelper outputHelper)
{
    [Fact(Skip = "Flaky")]
    public async Task StartAndGatherTcp()
    {
        await using var f = new WebApplicationFactory<Program>();
        f.WithWebHostBuilder(b =>
        {
            b.ConfigureLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddFakeLogging(o => { o.OutputSink = outputHelper.WriteLine; });
            });
        });
        var client = f.CreateClient();
        var s = await client.GetStringAsync("", TestContext.Current.CancellationToken);
        Assert.Equal("CPU : 0%\nIIS Requests : 0", s);
        Assert.True(true);
    }

    private static async Task AssertHttpReports(HttpClient client)
    {
        var response = await client.GetAsync("");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("CPU", content);
        Assert.Contains("IIS Requests", content);
    }

    private static async Task AssertTcpReports()
    {
        using var client = new TcpClient("localhost", 4414);
        NetworkStream stream = client.GetStream();

        // request
        await stream.WriteAsync(Encoding.ASCII.GetBytes(""));
        // response
        var buffer = new byte[8];
        var bytesRead = await stream.ReadAsync(buffer.AsMemory());

        var response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
        Assert.StartsWith("up", response);
    }
}
#pragma warning restore S1114
#pragma warning disable S125
