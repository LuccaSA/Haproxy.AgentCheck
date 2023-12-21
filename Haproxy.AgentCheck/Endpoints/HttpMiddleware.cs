using System.Net;
using Lucca.Infra.Haproxy.AgentCheck.Metrics;
using Microsoft.AspNetCore.Mvc;

namespace Lucca.Infra.Haproxy.AgentCheck.Endpoints;

internal static class HttpMiddleware
{
    public static IResult Invoke([FromServices] State state)
    {
        IEnumerable<string> lines = [];
        if (state.System is not null)
        {
            lines = lines.Concat(new []
            {
                $"CPU : {state.System?.CpuPercent}%",
                $"IIS Requests : {state.System?.IisRequests}"
            });
        }

        if (state.Counters is not null)
        {
            lines = lines.Concat(state.Counters.Values.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
        }

        var text = string.Join('\n', lines);
        var statusCode = state.IsReady ? (int)HttpStatusCode.OK : (int)HttpStatusCode.ServiceUnavailable;
        return Results.Text(text, statusCode: statusCode);
    }
}
