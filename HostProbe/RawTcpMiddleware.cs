using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Options;

namespace HostProbe
{
    public class RawTcpMiddleware : ConnectionHandler
    {
        private readonly State _state;
        private readonly IOptionsMonitor<HostProbeConfig> _options;

        public RawTcpMiddleware(State state, IOptionsMonitor<HostProbeConfig> options)
        {
            _state = state;
            _options = options;
        }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            ReadOnlyMemory<byte> AsReadOnlyMemory(string str)
            {
                return Encoding.ASCII.GetBytes(str).AsMemory();
            }
            async Task ReportUsageTcpAsync()
            {
                int max = _options.CurrentValue.CpuLimit;
                int weight = (int)((max - _state.CpuPercent) / (double)max * 100);
                if (weight == 0)
                {
                    // security to avoid full drain mode
                    weight = 1;
                }
                await connection.Transport.Output.WriteAsync(AsReadOnlyMemory($"up {weight}%\n"));
                await connection.Transport.Output.FlushAsync();
            }

            await ReportUsageTcpAsync();
        }
    }
}