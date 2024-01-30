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
                .GroupBy(kvp => GetCounterSource(kvp.Key))
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key,
                    g => g
                        .OrderBy(i => i.Key)
                        .ToDictionary(p => GetCounterName(p.Key), p => p.Value))
        });
    }

    private static string GetCounterSource(string fullName)
    {
        return fullName[..fullName.IndexOf('/', StringComparison.InvariantCultureIgnoreCase)];
    }

    private static string GetCounterName(string fullName)
    {
        return fullName[(fullName.IndexOf('/', StringComparison.InvariantCultureIgnoreCase) + 1)..];
    }
}
