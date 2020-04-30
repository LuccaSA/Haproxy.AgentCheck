using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Haproxy.AgentCheck
{
    public class BackgroundWatcher : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly IOptionsMonitor<AgentCheckConfig> _options;
        private IDisposable _disposableOptionHandler;
        private readonly StateCollector _stateCollector;
        public BackgroundWatcher(IOptionsMonitor<AgentCheckConfig> options,   StateCollector stateCollector)
        {
            _options = options;
            _stateCollector = stateCollector; 
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _disposableOptionHandler = _options.OnChange((x,y)=> UpdateTimer());
            UpdateTimer();
            return Task.CompletedTask;
        }

        private void UpdateTimer()
        {
            var oldTimer = _timer;
            oldTimer?.Dispose();
            var span = TimeSpan.FromMilliseconds(_options.CurrentValue.RefreshIntervalInMs);
            _timer = new Timer(OnTick, null, TimeSpan.Zero, span);
        }

        private void OnTick(object state)
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