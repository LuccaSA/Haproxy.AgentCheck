using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace HostProbe
{
    public class RawTcpMiddleware : ConnectionHandler
    {
        private readonly State _state;

        public RawTcpMiddleware(State state)
        {
            _state = state;
        }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            ReadOnlyMemory<byte> AsReadOnlyMemory(string str)
            {
                return Encoding.ASCII.GetBytes(str).AsMemory();
            }
            async Task ReportUsageTcpAsync()
            {
                await connection.Transport.Output.WriteAsync(AsReadOnlyMemory($"{_state.CpuPercent}%\n"));
                await connection.Transport.Output.FlushAsync();
            }

            await ReportUsageTcpAsync();
        }
    }
}