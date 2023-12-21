using System.Text;
using Lucca.Infra.Haproxy.AgentCheck.Metrics;
using Microsoft.AspNetCore.Connections;

namespace Lucca.Infra.Haproxy.AgentCheck.Endpoints;

internal class TcpMiddleware(State state) : ConnectionHandler
{
    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        await connection.Transport.Output.WriteAsync(Encoding.ASCII.GetBytes($"{(state.IsReady ? "ready" : "maint")} {state.Weight}%\n").AsMemory(), connection.ConnectionClosed);
        await connection.Transport.Output.FlushAsync(connection.ConnectionClosed);
    }
}
