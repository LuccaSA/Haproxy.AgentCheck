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
            async Task ReportUsage()
            {
                await context.Response.WriteAsync($"{_state.CpuPercent}%\n");
                //await context.Response.WriteAsync($"IIS number of queries {_state.IisRequests}\n");
            }

            var config = _options.CurrentValue;
            if (_state.CpuPercent > config.CpuLimit)
            {
                context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                await ReportUsage();
                return;
            }

            if (_state.IisRequests > config.IisRequestsLimit)
            {
                context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                await ReportUsage();
                return;
            }

            //await context.Response.WriteAsync("Looks good\n");
            await ReportUsage();
        }
    }
}