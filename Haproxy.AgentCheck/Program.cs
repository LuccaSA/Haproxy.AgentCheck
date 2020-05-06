using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Runtime.InteropServices;

namespace Haproxy.AgentCheck
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSystemService()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseStartup<Startup>()
                        .UseKestrelOnPorts(8042, 4243);
                });
    }

    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseSystemService(this IHostBuilder hostBuilder)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                hostBuilder.UseWindowsService();
                return hostBuilder;
            }
            throw new NotImplementedException("TODO : SystemD");
        }

        public static IWebHostBuilder UseKestrelOnPorts(this IWebHostBuilder webHostBuilder, int http, int tcp)
        {
            return webHostBuilder.UseKestrel((builder, options) =>
            {
                // TCP 4243
                options.ListenAnyIP(tcp, opt =>
                {
                    opt.UseConnectionHandler<TcpMiddleware>();
                });
                // HTTP 8042
                options.ListenAnyIP(http);
            });
        }
    }
}
