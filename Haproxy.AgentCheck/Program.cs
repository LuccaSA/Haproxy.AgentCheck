using Lucca.Infra.Haproxy.AgentCheck.Config;
using Lucca.Infra.Haproxy.AgentCheck.Endpoints;
using Lucca.Infra.Haproxy.AgentCheck.Hosting;
using Lucca.Infra.Haproxy.AgentCheck.Metrics;

Environment.SetEnvironmentVariable("DD_TRACE_ENABLED", "0", EnvironmentVariableTarget.Process);
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSystemService();
builder.WebHost.UseKestrelOnPorts(8042, 4243);
builder.Services.AddHostedService<ProcessCountersBackgroundService>();
builder.Services.AddHostedService<SystemCountersBackgroundService>();
builder.Services.AddOptions<WatchConfig>()
    .BindConfiguration("Watch")
    .ValidateOnStart()
    .ValidateDataAnnotations();
builder.Services.AddOptions<RulesConfig>()
    .BindConfiguration("Rules")
    .ValidateOnStart()
    .ValidateDataAnnotations();
builder.Services.AddMetricCollector();
builder.Services.AddSingleton<State>();
var app = builder.Build();
app.MapGet("", HttpMiddleware.Invoke);
await app.RunAsync();

#pragma warning disable S1118
namespace Lucca.Infra.Haproxy.AgentCheck
{
    public partial class Program
    {
    }
}
#pragma warning restore S1118
