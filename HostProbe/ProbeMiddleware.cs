using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace HostProbe
{
    public class ProbeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly State _state;
        private readonly IOptionsMonitor<HostProbeConfig> _options;
        public ProbeMiddleware(RequestDelegate next, State state, IOptionsMonitor<HostProbeConfig> options)
        {
            _next = next;
            _state = state;
            _options = options;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var config = _options.CurrentValue;
            if (_state.CpuPercent > config.CpuLimit)
            {
                context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                await context.Response.WriteAsync("CPU at " + _state.CpuPercent);
                return;
            }

            if (_state.IisRequests > config.IisRequestsLimit)
            {
                context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                await context.Response.WriteAsync("IIS number of queries " + _state.IisRequests);
                return;
            }

            await context.Response.WriteAsync("Looks good");
        }
    }
}