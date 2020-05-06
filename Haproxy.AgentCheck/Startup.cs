using System;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Haproxy.AgentCheck
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHostedService<BackgroundWatcher>();

            services.Configure<AgentCheckConfig>(_configuration.GetSection("AgentCheckConfig"));
            OptionsOverride(services);
            services.ValidateConfig();

            services.AddMetricCollector();
            services.AddSingleton<StateProjection>();
        }

        protected virtual void OptionsOverride(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<HttpMiddleware>();
        }
    }

    internal static class StartupExtensions
    {
        internal static void AddMetricCollector(this IServiceCollection services)
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
}
