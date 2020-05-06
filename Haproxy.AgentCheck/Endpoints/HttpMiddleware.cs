using System;
using System.Net;
using System.Threading.Tasks;
using Haproxy.AgentCheck.Metrics;
using Microsoft.AspNetCore.Http;

namespace Haproxy.AgentCheck.Endpoints
{
    public class HttpMiddleware
    {
        private readonly StateProjection _stateProjection;

        public HttpMiddleware(RequestDelegate _, StateProjection computeLimits)
        {
            _stateProjection = computeLimits;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.Response.StatusCode = _stateProjection.IsServiceAvailable ? (int)HttpStatusCode.OK : (int)HttpStatusCode.ServiceUnavailable;
            await context.Response.WriteAsync($"CPU : {_stateProjection.State.CpuPercent}%\n");
            await context.Response.WriteAsync($"Requests : {_stateProjection.State.IisRequests}\n");
        }
    }
}
