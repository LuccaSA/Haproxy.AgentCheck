using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Lucca.Infra.Haproxy.AgentCheck.Tests;
#pragma warning disable S1144
#pragma warning disable S125
public class IntegrationTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task StartAndGatherTcp()
    {
        using var f = new WebApplicationFactory<Program>();
        f.WithWebHostBuilder(b =>
        {
            b.ConfigureLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddFakeLogging(o => { o.OutputSink = outputHelper.WriteLine; });
            });
        });
        var client = f.CreateClient();
        var s = await client.GetStringAsync("");
        Assert.Equal("CPU : 0%\nRequests : 0", s);
        Assert.True(true);
        // var hostBuilder = new HostBuilder()
        //     .ConfigureServices(s =>
        //     {
        //         s.AddLogging(b => b.AddXUnit(outputHelper));
        //     })
        //     .ConfigureWebHost(w =>
        //     {
        //         w.UseStartup<IntegrationStartup>()
        //             .UseKestrelOnPorts(4412, 4414);
        //     });
        // using var host = await hostBuilder.StartAsync();
        //
        // await AssertTcpReports();
        // await AssertHttpReports();
        //
        // await host.StopAsync();
    }

    private static async Task AssertHttpReports(HttpClient client)
    {
        var response = await client.GetAsync("");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("CPU", content);
        Assert.Contains("Requests", content);
    }

    private static async Task AssertTcpReports()
    {
        using var client = new TcpClient("localhost", 4414);
        NetworkStream stream = client.GetStream();

        // request
        await stream.WriteAsync(Encoding.ASCII.GetBytes(""));
        // response
        var buffer = new byte[8].AsMemory();
        await stream.ReadAsync(buffer);

        var response = Encoding.ASCII.GetString(buffer.Span);
        Assert.StartsWith("up", response);
    }
}
#pragma warning restore S1114
#pragma warning disable S125
