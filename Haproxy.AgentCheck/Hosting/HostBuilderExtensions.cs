using System.Runtime.InteropServices;
using Lucca.Infra.Haproxy.AgentCheck.Endpoints;
using Lucca.Infra.Haproxy.AgentCheck.Metrics;
using Microsoft.AspNetCore.Connections;

namespace Lucca.Infra.Haproxy.AgentCheck.Hosting;

internal static class HostBuilderExtensions
{
    public static void UseSystemService(this IHostBuilder hostBuilder)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            hostBuilder.UseWindowsService();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            hostBuilder.UseSystemd();
        }
        else
        {
            throw new NotSupportedException("Unsupported platform");
        }
    }

    public static void UseKestrelOnPorts(this IWebHostBuilder webHostBuilder, int http, int tcp)
    {
        webHostBuilder.UseKestrel(options =>
        {
            options.ListenAnyIP(tcp, opt =>
            {
                opt.UseConnectionHandler<TcpHandler>();
            });
            options.ListenAnyIP(http);
        });
    }

    public static void AddMetricCollector(this IServiceCollection services)
    {
        services.AddSingleton<State>();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddSingleton<IStateCollector, WindowsStateCollector>();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            services.AddSingleton<IStateCollector, LinuxStateCollector>();
        }
        else
        {
            throw new PlatformNotSupportedException("Only windows and linux platforms are supported");
        }
    }
}
