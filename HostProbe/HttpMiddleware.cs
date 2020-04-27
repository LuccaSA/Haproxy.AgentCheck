using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace HostProbe
{
    public class HttpMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly StateProjection _stateProjection;

        public HttpMiddleware(RequestDelegate next, StateProjection computeLimits)
        {
            _next = next;
            _stateProjection = computeLimits;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.StatusCode = _stateProjection.IsServiceAvailable ? (int)HttpStatusCode.OK : (int)HttpStatusCode.ServiceUnavailable;
            await context.Response.WriteAsync($"CPU : {_stateProjection.State.CpuPercent}%\n");
            await context.Response.WriteAsync($"Requests : {_stateProjection.State.IisRequests}\n");
        }
    }
}