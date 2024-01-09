#pragma warning disable CA1506
using Lucca.Infra.Haproxy.AgentCheck;
using Lucca.Infra.Haproxy.AgentCheck.Config;
using Lucca.Infra.Haproxy.AgentCheck.Endpoints;
using Lucca.Infra.Haproxy.AgentCheck.Hosting;
using Lucca.Infra.Haproxy.AgentCheck.Metrics;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Core;

Environment.SetEnvironmentVariable("DD_TRACE_ENABLED", "0", EnvironmentVariableTarget.Process);
var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddSerilog(CreateLoggerConfiguration(builder.Configuration));
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
builder.Services.AddSingleton<MaintenanceStatus>();
builder.Services.AddSingleton(TimeProvider.System);
var app = builder.Build();
app.MapGet("", HttpMiddleware.Invoke);
app.MapPost("/maintenance", ([FromServices]MaintenanceStatus status) =>
{
    status.IsMaintenance = true;
});
app.MapPost("/ready", ([FromServices]MaintenanceStatus status) =>
{
    status.IsMaintenance = false;
});
await app.RunAsync();

static Logger CreateLoggerConfiguration(IConfiguration configuration)
{
    return new LoggerConfiguration()
        .Enrich.WithProcessId()
        .Enrich.WithProcessName()
        .Enrich.WithAssemblyName()
        .Enrich.WithAssemblyInformationalVersion()
        .Enrich.WithMachineName()
        .ReadFrom.Configuration(configuration)
        .CreateLogger();
}

#pragma warning restore CA1506
#pragma warning disable S1118
namespace Lucca.Infra.Haproxy.AgentCheck
{
    public partial class Program
    {
    }

    public class MaintenanceStatus
    {
        public bool IsMaintenance { get; set; }
    }
}
#pragma warning restore S1118
