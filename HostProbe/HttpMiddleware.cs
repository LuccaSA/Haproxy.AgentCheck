using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace HostProbe
{
    public class HttpMiddleware
    {
        private readonly StateProjection _stateProjection;

        public HttpMiddleware(StateProjection computeLimits)
        {
            _stateProjection = computeLimits;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.StatusCode = _stateProjection.IsServiceAvailable ? (int)HttpStatusCode.OK : (int)HttpStatusCode.ServiceUnavailable;
            await context.Response.WriteAsync($"CPU : {_stateProjection.State.CpuPercent}%\n");
            await context.Response.WriteAsync($"Requests : {_stateProjection.State.IisRequests}%\n");
        }
    }
}