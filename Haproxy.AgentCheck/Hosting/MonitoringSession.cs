using System.Globalization;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;

namespace Lucca.Infra.Haproxy.AgentCheck.Hosting;

#pragma warning disable S3881
internal partial class MonitoringSession
{
    private readonly IEnumerable<EventPipeProvider> _providers;
    private readonly ILogger _logger;
    private readonly Dictionary<string, int> _counters = [];

    public MonitoringSession(string processName, int processId, IEnumerable<EventPipeProvider> providers, ILogger logger)
    {
        ProcessName = processName;
        ProcessId = processId;
        _providers = providers;
        _logger = logger;
    }

    public string ProcessName { get; }
    public int ProcessId { get; }
    public event Action<MonitoringSession, Dictionary<string, int>>? CountersUpdated;
    public event Action<MonitoringSession>? SessionEnded;

    public async Task ListenAsync(CancellationToken cancellationToken)
    {
        var diagnosticsClient = new DiagnosticsClient(ProcessId);
        using var session = diagnosticsClient.StartEventPipeSession(_providers);
        using var source = new EventPipeEventSource(session.EventStream);
        cancellationToken.Register(() =>
        {
            source.StopProcessing();
            session.Stop();
        });

        source.Dynamic.All += OnTraceEvent;

        try
        {
            await Task.Factory.StartNew(() => source.Process(), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
        catch (Exception e)
        {
            LogProcessingException(_logger, e);
        }
        finally
        {
            source.Dynamic.All -= OnTraceEvent;
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
            _counters[$"{providerName}/{(string)payload["Name"]}"] =
                payload["Mean"] switch
                {
                    long and > int.MaxValue => int.MaxValue,
                    float and > int.MaxValue => int.MaxValue,
                    double and > int.MaxValue => int.MaxValue,
                    int i => i,
                    _ => Convert.ToInt32(payload["Mean"], CultureInfo.InvariantCulture)
                };
            CountersUpdated?.Invoke(this, _counters);
        }
    }
}
#pragma warning restore S3881
