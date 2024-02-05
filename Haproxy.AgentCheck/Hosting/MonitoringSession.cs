using System.Globalization;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;

namespace Lucca.Infra.Haproxy.AgentCheck.Hosting;

#pragma warning disable S3881
internal partial class MonitoringSession
{
    private readonly IEnumerable<EventPipeProvider> _providers;
    private readonly ILogger _logger;
    private readonly Dictionary<string, double> _counters = [];

    public MonitoringSession(string processName, int processId, IEnumerable<EventPipeProvider> providers, ILogger logger)
    {
        ProcessName = processName;
        ProcessId = processId;
        _providers = providers;
        _logger = logger;
    }

    public string ProcessName { get; }
    public int ProcessId { get; }
    public event Action<MonitoringSession, Dictionary<string, double>>? CountersUpdated;
    public event Action<MonitoringSession>? SessionEnded;

    public async Task ListenAsync(CancellationToken cancellationToken)
    {
        EventPipeSession? session = default;
        EventPipeEventSource? source = default;

        try
        {
            var diagnosticsClient = new DiagnosticsClient(ProcessId);
            session = diagnosticsClient.StartEventPipeSession(_providers);
            source = new EventPipeEventSource(session.EventStream);
            await using var registration = cancellationToken.Register(() =>
            {
                source?.StopProcessing();
                session?.Stop();
            });

            source.Dynamic.All += OnTraceEvent;

            await Task.Factory.StartNew(() => source.Process(), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
        catch (Exception e)
        {
            LogProcessingException(_logger, e);
        }
        finally
        {
            if (source is not null)
            {
                source.Dynamic.All -= OnTraceEvent;
            }
            source?.Dispose();
            session?.Dispose();
            SessionEnded?.Invoke(this);
        }
    }

    private void OnTraceEvent(TraceEvent obj)
    {
        if (!obj.EventName.Equals("EventCounters", StringComparison.OrdinalIgnoreCase)) return;

        var providerName = obj.ProviderName;
        var payload = (IDictionary<string, object>)((IDictionary<string, object>)obj.PayloadValue(0))["Payload"];
        if (((string)payload["CounterType"]).Equals("Mean", StringComparison.OrdinalIgnoreCase))
        {
            _counters[$"{providerName}/{(string)payload["Name"]}"] = Convert.ToDouble(payload["Mean"], CultureInfo.InvariantCulture);
            CountersUpdated?.Invoke(this, _counters);
        }
    }
}
#pragma warning restore S3881
