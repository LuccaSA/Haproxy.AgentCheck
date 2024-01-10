using Lucca.Infra.Haproxy.AgentCheck.Metrics;
using Microsoft.AspNetCore.Mvc;

namespace Lucca.Infra.Haproxy.AgentCheck.Endpoints;

internal static class HttpHandler
{
    public static IResult Invoke([FromServices] State state, [FromServices] MaintenanceStatus maintenanceStatus)
    {
        return Results.Json(new
        {
            maintenanceStatus.IsMaintenance,
            state.IsUp,
            state.Weight,
            state.System,
            Counters = state.Counters?.Values
        });
    }
}
