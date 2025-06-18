using System.Text;
using Lucca.Infra.Haproxy.AgentCheck.Metrics;
using Microsoft.AspNetCore.Connections;

namespace Lucca.Infra.Haproxy.AgentCheck.Endpoints;

internal class TcpHandler(State state, MaintenanceStatus maintenanceStatus) : ConnectionHandler
{
    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(connection);

            var up = state.IsUp ? "up" : "down";
            if (!state.IsUp)
            {
                up += $" #{state.BrokenCircuitsBreakers.Count} failures ({string.Join(",", state.BrokenCircuitsBreakers)})";
            }

            if (maintenanceStatus.IsMaintenance)
            {
                up = "stopped #Requested maintenance";
            }

            await connection.Transport.Output.WriteAsync(Encoding.ASCII.GetBytes($"{state.Weight:F0}% {up}\n").AsMemory(), connection.ConnectionClosed);
            await connection.Transport.Output.FlushAsync(connection.ConnectionClosed);

            await connection.Transport.Output.CompleteAsync();
            await connection.Transport.Input.CompleteAsync();
        }
        catch (TaskCanceledException) when (connection.ConnectionClosed.IsCancellationRequested)
        {
            // ignore when connection is closed
        }
    }
}
