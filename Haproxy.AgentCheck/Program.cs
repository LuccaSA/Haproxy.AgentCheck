using Microsoft.AspNetCore.Hosting;
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
                .UseSystemService()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseStartup<Startup>()
                        .UseKestrelOnPorts(8042, 4243);
                });
    }
}
