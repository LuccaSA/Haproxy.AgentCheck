using Microsoft.AspNetCore.Mvc;

namespace Lucca.Infra.Haproxy.AgentCheck.Endpoints;

internal partial class MaintenanceHandler
{
    public static void SetInMaintenance([FromServices] MaintenanceStatus maintenanceStatus, [FromServices] ILogger<MaintenanceHandler> logger)
    {
        maintenanceStatus.IsMaintenance = true;
        LogInMaintenance(logger);
    }

    public static void SetReady([FromServices] MaintenanceStatus maintenanceStatus, [FromServices] ILogger<MaintenanceHandler> logger)
    {
        maintenanceStatus.IsMaintenance = false;
        LogReady(logger);
    }

    [LoggerMessage(LogLevel.Information, "Server marked in maintenance")]
    private static partial void LogInMaintenance(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Server marked ready")]
    private static partial void LogReady(ILogger logger);
}
