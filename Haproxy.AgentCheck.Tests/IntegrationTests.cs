using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Haproxy.AgentCheck.Config;
using Haproxy.AgentCheck.Hosting;
using Haproxy.AgentCheck.Metrics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Haproxy.AgentCheck.Tests
{
    public class IntegrationTests
    {
        [Fact]
        public async Task StartAndGatherTcp()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(w =>
                {
                    w.UseStartup<IntegrationStartup>()
                        .UseKestrelOnPorts(4412, 4414);
                });
            using var host = await hostBuilder.StartAsync();
            await Task.Delay(TimeSpan.FromSeconds(2));

            await AssertTcpReports();
            await AssertHttpReports();

            await host.StopAsync();
        }

        private async Task AssertHttpReports()
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:4412");

            var response = await client.GetAsync(new Uri("/"));
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

    public class IntegrationStartup : Startup
    {
        public IntegrationStartup(IConfiguration configuration)
            : base(configuration)
        {
        }
        protected override void OptionsOverride(IServiceCollection services)
        {
            services.ConfigureOptions<TestAgentCheckConfig>();
        }
    }

    public class TestAgentCheckConfig : IConfigureOptions<AgentCheckConfig>
    {
        public void Configure(AgentCheckConfig options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            options.CpuLimit = 42;
            options.IisRequestsLimit = 42;
            options.RefreshIntervalInMs = 200;
            options.SystemResponse = SystemResponse.FirstOrder;
        }
    }
}
