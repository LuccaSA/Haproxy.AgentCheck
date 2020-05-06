using System;
using System.Text;
using System.Threading.Tasks;
using Haproxy.AgentCheck.Metrics;
using Microsoft.AspNetCore.Connections;

namespace Haproxy.AgentCheck.Endpoints
{
    public class TcpMiddleware : ConnectionHandler
    {
        private readonly StateProjection _stateProjection;

        public TcpMiddleware(StateProjection computeLimits)
        {
            _stateProjection = computeLimits;
        }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            await connection.Transport.Output.WriteAsync(Encoding.ASCII.GetBytes($"up {_stateProjection.Weight}%\n").AsMemory());
            await connection.Transport.Output.FlushAsync();
        }
    }
}
