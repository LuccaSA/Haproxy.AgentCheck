using System;
using Haproxy.AgentCheck.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Haproxy.AgentCheck
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Environment.SetEnvironmentVariable("DD_TRACE_ENABLED", "0", EnvironmentVariableTarget.Process);
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
