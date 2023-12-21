using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using Lucca.Infra.Haproxy.AgentCheck.Config;
using Lucca.Infra.Haproxy.AgentCheck.Metrics;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Extensions.Options;

namespace Lucca.Infra.Haproxy.AgentCheck.Hosting;

internal partial class ProcessCountersBackgroundService(
    State state,
    IOptionsMonitor<WatchConfig> optionsMonitor,
    ILogger<ProcessCountersBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var eventPipeArgs = new Dictionary<string, string>
        {
            ["EventCounterIntervalSec"] = "1"
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            var processName = optionsMonitor.CurrentValue.Counters.Process;
            var pids = DiagnosticsClient.GetPublishedProcesses()
                .Select(Process.GetProcessById)
                .Where(p => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Id)
                .ToList();

            int pid;
            switch (pids)
            {
                case { Count: 0 }:
                    LogNoProcessRunning(logger, processName);
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                    continue;
                case { Count: 1 }:
                    pid = pids[0];
                    LogOneProcessRunning(logger, processName, pid);
                    break;
                default:
                    pid = pids[0];
                    LogMoreThanOneProcessRunning(logger, processName, pid, pids.Count);
                    break;
            }

            var providers = optionsMonitor.CurrentValue.Counters.Providers
                .Select(name => new EventPipeProvider(name, EventLevel.Informational, (long)ClrTraceEventParser.Keywords.None, eventPipeArgs))
                .ToList();

            var diagnosticsClient = new DiagnosticsClient(pid);
            using var session = diagnosticsClient.StartEventPipeSession(providers);
            using var source = new EventPipeEventSource(session.EventStream);
            stoppingToken.Register(() =>
            {
                source.StopProcessing();
                session.Stop();
            });
            var counters = new Dictionary<string, int>();
            source.Dynamic.All += OnTraceEvent;

            try
            {
                await Task.Factory.StartNew(() => source.Process(), stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            catch (Exception e)
            {
                LogProcessingException(logger, e);
            }
            finally
            {
                source.Dynamic.All -= OnTraceEvent;
            }

            void OnTraceEvent(TraceEvent obj)
            {
                if (!obj.EventName.Equals("EventCounters", StringComparison.OrdinalIgnoreCase)) return;

                var providerName = obj.ProviderName;
                var payload = (IDictionary<string, object>)((IDictionary<string, object>)obj.PayloadValue(0))["Payload"];
                if (((string)payload["CounterType"]).Equals("Mean", StringComparison.OrdinalIgnoreCase))
                {
                    counters[$"{providerName}/{(string)payload["Name"]}"] = Convert.ToInt32(payload["Mean"], CultureInfo.InvariantCulture);
                    state.UpdateState(new CountersState { Values = counters });
                }
            }
        }
    }
}
