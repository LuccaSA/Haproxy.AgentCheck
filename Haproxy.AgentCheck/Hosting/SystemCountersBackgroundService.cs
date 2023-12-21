using Lucca.Infra.Haproxy.AgentCheck.Config;
using Lucca.Infra.Haproxy.AgentCheck.Metrics;
using Microsoft.Extensions.Options;

namespace Lucca.Infra.Haproxy.AgentCheck.Hosting;

internal sealed class SystemCountersBackgroundService(IOptionsMonitor<WatchConfig> optionsMonitor, IStateCollector stateCollector)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var timer = new Timer(OnTick);
        using var optionSubscription = optionsMonitor.OnChange((_, __) => UpdateTimer(timer));
        UpdateTimer(timer);
        var tcs = new TaskCompletionSource();
        await using var cancellationTokenRegistration = stoppingToken.Register(() => tcs.SetResult());
        await tcs.Task;
    }

    private void UpdateTimer(Timer timer)
    {
        var period = TimeSpan.FromMilliseconds(optionsMonitor.CurrentValue.System.RefreshIntervalInMs);
        timer.Change(TimeSpan.Zero, period);
    }

    private void OnTick(object? state)
    {
        stateCollector.Collect();
    }
}
