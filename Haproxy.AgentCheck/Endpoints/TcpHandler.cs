using System.Text;
using Lucca.Infra.Haproxy.AgentCheck.Metrics;
using Microsoft.AspNetCore.Connections;

namespace Lucca.Infra.Haproxy.AgentCheck.Endpoints;

internal class TcpHandler(State state, MaintenanceStatus maintenanceStatus) : ConnectionHandler
{
    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var up = state.IsUp ? "up" : "down";
        if (!state.IsUp)
        {
            up += $"#{state.BrokenCircuitsBreakers.Count} failures ({string.Join(",", state.BrokenCircuitsBreakers)})";
        }

        var isMaintenance = maintenanceStatus.IsMaintenance ? "maint" : "ready";

        await connection.Transport.Output.WriteAsync(Encoding.ASCII.GetBytes($"{state.Weight}% {isMaintenance} {up}\n").AsMemory(), connection.ConnectionClosed);
        await connection.Transport.Output.FlushAsync(connection.ConnectionClosed);
    }
}
