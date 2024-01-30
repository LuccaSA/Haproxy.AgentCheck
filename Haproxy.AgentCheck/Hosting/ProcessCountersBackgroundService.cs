using System.Diagnostics;
using System.Diagnostics.Tracing;
using Lucca.Infra.Haproxy.AgentCheck.Config;
using Lucca.Infra.Haproxy.AgentCheck.Metrics;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Extensions.Options;

namespace Lucca.Infra.Haproxy.AgentCheck.Hosting;

internal partial class ProcessCountersBackgroundService(
    State state,
    IOptionsMonitor<WatchConfig> watchConfigMonitor,
    IOptionsMonitor<RulesConfig> rulesConfigMonitor,
    ILogger<ProcessCountersBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(watchConfigMonitor.CurrentValue.Process))
        {
            return;
        }

        var eventPipeArgs = new Dictionary<string, string>
        {
            ["EventCounterIntervalSec"] = "1"
        };

        bool? processFound = default;

        while (!stoppingToken.IsCancellationRequested)
        {
            var providers = rulesConfigMonitor.CurrentValue
                .Where(rule => rule.Source == RuleSource.Counters)
                .Select(rule => rule.Name.Split('/')[0])
                .Concat(watchConfigMonitor.CurrentValue.DataSources)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .Select(name => new EventPipeProvider(name, EventLevel.Informational, (long)ClrTraceEventParser.Keywords.None, eventPipeArgs))
                .ToList();

            if (providers.Count == 0)
            {
                return;
            }

            var processName = watchConfigMonitor.CurrentValue.Process;
            var processes = DiagnosticsClient.GetPublishedProcesses()
                .Select(Process.GetProcessById)
                .Where(p => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                .Select(p => (p.ProcessName, ProcessId: p.Id))
                .ToList();

            (string ProcessName, int ProcessId) process;
            switch (processes)
            {
                case { Count: 0 }:
                    if (processFound != false)
                    {
                        LogNoProcessRunning(logger, processName);
                    }
                    processFound = false;
                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                    continue;
                case { Count: 1 }:
                    processFound = true;
                    process = processes[0];
                    LogOneProcessRunning(logger, process.ProcessName, process.ProcessId);
                    break;
                default:
                    processFound = true;
                    process = processes[0];
                    LogMoreThanOneProcessRunning(logger, process.ProcessName, process.ProcessId, processes.Count);
                    break;
            }

            var session = new MonitoringSession(process.ProcessName, process.ProcessId, providers, logger);
            session.CountersUpdated += OnCountersUpdated;
            await session.ListenAsync(stoppingToken);

            void OnCountersUpdated(MonitoringSession monitoringSession, Dictionary<string, double> counters)
            {
                state.UpdateState(new CountersState { Values = counters });
            }
        }
    }
}

internal partial class ProcessCountersBackgroundService
{
    [LoggerMessage(LogLevel.Error, "No process {processName} currently running.")]
    public static partial void LogNoProcessRunning(ILogger logger, string processName);

    [LoggerMessage(LogLevel.Warning, "Found {count} {processName} currently running, watching pid {pid}.")]
    public static partial void LogMoreThanOneProcessRunning(ILogger logger, string processName, int pid, int count);

    [LoggerMessage(LogLevel.Information, "Watching {processName} pid {pid}.")]
    public static partial void LogOneProcessRunning(ILogger logger, string processName, int pid);
}
