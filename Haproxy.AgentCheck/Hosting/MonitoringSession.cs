using System.Globalization;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;

namespace Lucca.Infra.Haproxy.AgentCheck.Hosting;

#pragma warning disable S3881
internal partial class MonitoringSession : IDisposable
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, int> _counters = [];
    private readonly EventPipeSession _session;
    private readonly EventPipeEventSource _source;

    public MonitoringSession(string processName, int processId, IEnumerable<EventPipeProvider> providers, ILogger logger)
    {
        ProcessName = processName;
        ProcessId = processId;
        _logger = logger;
        var diagnosticsClient = new DiagnosticsClient(ProcessId);
        _session = diagnosticsClient.StartEventPipeSession(providers);
        _source = new EventPipeEventSource(_session.EventStream);
    }

    public string ProcessName { get; }
    public int ProcessId { get; }
    public event Action<MonitoringSession, Dictionary<string, int>>? CountersUpdated;
    public event Action<MonitoringSession>? SessionEnded;

    public async Task ListenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.Register(() =>
        {
            _source.StopProcessing();
            _session.Stop();
        });

        _source.Dynamic.All += OnTraceEvent;

        try
        {
            await Task.Factory.StartNew(() => _source.Process(), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
        catch (Exception e)
        {
            LogProcessingException(_logger, e);
        }
        finally
        {
            _source.Dynamic.All -= OnTraceEvent;
            SessionEnded?.Invoke(this);
            Dispose();
        }
    }

    private void OnTraceEvent(TraceEvent obj)
    {
        if (!obj.EventName.Equals("EventCounters", StringComparison.OrdinalIgnoreCase)) return;

        var providerName = obj.ProviderName;
        var payload = (IDictionary<string, object>)((IDictionary<string, object>)obj.PayloadValue(0))["Payload"];
        if (((string)payload["CounterType"]).Equals("Mean", StringComparison.OrdinalIgnoreCase))
        {
            _counters[$"{providerName}/{(string)payload["Name"]}"] = Convert.ToInt32(payload["Mean"], CultureInfo.InvariantCulture);
            CountersUpdated?.Invoke(this, _counters);
        }
    }

    public void Dispose()
    {
        _session.Dispose();
        _source.Dispose();
    }
}
#pragma warning restore S3881
