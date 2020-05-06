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

            services.AddSingleton<StateCollector>();
            services.AddSingleton<State>();
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
}
