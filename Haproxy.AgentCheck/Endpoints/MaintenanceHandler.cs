using System.Diagnostics.Metrics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Lucca.Infra.Haproxy.AgentCheck.Endpoints;

internal partial class MaintenanceHandler
{
    private static readonly Gauge<int> MaintenanceGauge = Instrumentation.Sources.Meter.CreateGauge<int>("agent_check_maintenance");

    static MaintenanceHandler()
    {
        MaintenanceGauge.Record(0);
    }

    public static void SetInMaintenance([FromServices] MaintenanceStatus maintenanceStatus, [FromServices] ILogger<MaintenanceHandler> logger, ClaimsPrincipal principal)
    {
        maintenanceStatus.IsMaintenance = true;
        MaintenanceGauge.Record(1);
        LogInMaintenance(logger, principal.Identity!.Name!);
    }

    public static void SetReady([FromServices] MaintenanceStatus maintenanceStatus, [FromServices] ILogger<MaintenanceHandler> logger, ClaimsPrincipal principal)
    {
        maintenanceStatus.IsMaintenance = false;
        MaintenanceGauge.Record(0);
        LogReady(logger, principal.Identity!.Name!);
    }

    [LoggerMessage(LogLevel.Information, "Server marked in maintenance by {User}")]
    private static partial void LogInMaintenance(ILogger logger, string user);

    [LoggerMessage(LogLevel.Information, "Server marked ready by {User}")]
    private static partial void LogReady(ILogger logger, string user);
}
