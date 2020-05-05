using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
                .UseWindowsService()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseStartup<Startup>()
                        .UseKestrel(options =>
                        {
                            // TCP 4243
                            options.ListenAnyIP(4243, opt =>
                            {
                                opt.UseConnectionHandler<TcpMiddleware>();

                            });
                            // HTTP 5000
                            options.ListenAnyIP(8042);
                        });

                });
    }
}
