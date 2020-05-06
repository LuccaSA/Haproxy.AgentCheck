using System;
using System.Threading;
using System.Threading.Tasks;
using Haproxy.AgentCheck.Config;
using Haproxy.AgentCheck.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Haproxy.AgentCheck.Hosting
{
    public sealed class BackgroundWatcher : IHostedService, IDisposable
    {
        private Timer? _timer;
        private readonly IOptionsMonitor<AgentCheckConfig> _options;
        private IDisposable? _disposableOptionHandler;
        private readonly IStateCollector _stateCollector;

        public BackgroundWatcher(IOptionsMonitor<AgentCheckConfig> options, IStateCollector stateCollector)
        {
            _options = options;
            _stateCollector = stateCollector;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _disposableOptionHandler = _options.OnChange((_, __) => UpdateTimer());
            UpdateTimer();
            return Task.CompletedTask;
        }

        private void UpdateTimer()
        {
            var oldTimer = _timer;
            oldTimer?.Dispose();
            var period = TimeSpan.FromMilliseconds(_options.CurrentValue.RefreshIntervalInMs);
            _timer = new Timer(OnTick, null, TimeSpan.Zero, period);
        }

        private void OnTick(object? state)
        {
            _stateCollector.Collect();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _disposableOptionHandler?.Dispose();
        }
    }
}
