using System;
using System.Runtime.InteropServices;
using Haproxy.AgentCheck.Endpoints;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Haproxy.AgentCheck.Hosting
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseSystemService(this IHostBuilder hostBuilder)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                hostBuilder.UseWindowsService();
                return hostBuilder;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                hostBuilder.UseSystemd();
                return hostBuilder;
            }
            throw new NotSupportedException("Unsupported platform");
        }

        public static IWebHostBuilder UseKestrelOnPorts(this IWebHostBuilder webHostBuilder, int http, int tcp)
        {
            return webHostBuilder.UseKestrel((builder, options) =>
            {
                options.ListenAnyIP(tcp, opt =>
                {
                    opt.UseConnectionHandler<TcpMiddleware>();
                });
                options.ListenAnyIP(http);
            });
        }
    }
}
