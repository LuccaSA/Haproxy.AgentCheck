using System;
using System.IO;

namespace Haproxy.AgentCheck.Metrics
{
    public sealed class LinuxStateCollector : IStateCollector
    {
        private readonly State _state;
        private ProcStat _lastStat = ProcStat.Empty;

        public LinuxStateCollector(State state)
        {
            _state = state;
        }

        public void Collect()
        {
            using var reader = new StreamReader("/proc/stat");
            var stat = ProcStat.FromLine(reader.ReadLine());

            if (_lastStat != ProcStat.Empty)
                _state.CpuPercent = _lastStat.AverageCpuWith(stat);

            _lastStat = stat;
        }

        public void Dispose()
        {
            // nothing to dispose
        }
    }

    internal class ProcStat
    {
        private readonly int[] _stats;

        private ProcStat(int[] stats)
        {
            _stats = stats;
        }

        internal static ProcStat FromLine(string? firstProcStatLine)
        {
            if (firstProcStatLine == null)
                throw new ArgumentNullException(nameof(firstProcStatLine), "/proc/stat is returning invalid data");

            if (!firstProcStatLine.StartsWith("cpu ", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("/proc/stat is returning invalid first line", nameof(firstProcStatLine));

            var span = firstProcStatLine.AsSpan();
            span = span.Slice(3).TrimStart(' '); // "cpu"
            var stats = new int[7];

            for (int i = 0; i < 7; i++)
            {
                var nextSpace = span.IndexOf(' ');
                if (nextSpace == -1)
                    throw new ArgumentException("/proc/stat structure is invalid", nameof(firstProcStatLine));
                stats[i] = int.Parse(span.Slice(0, nextSpace));
                span = span.Slice(nextSpace + 1);
            }

            return new ProcStat(stats);
        }

        internal int AverageCpuWith(ProcStat with) => 100 - (int)Math.Floor((Idle - with.Idle) * 100 / (double)(Total - with.Total));

        public static ProcStat Empty { get; } = new ProcStat(new int[7]);

        /// <summary>
        /// normal processes executing in user mode
        /// </summary>
        public int User => _stats[0];
        /// <summary>
        /// niced processes executing in user mode
        /// </summary>
        public int Nice => _stats[1];
        /// <summary>
        /// processes executing in kernel mode
        /// </summary>
        public int System => _stats[2];
        /// <summary>
        /// twiddling thumbs
        /// </summary>
        public int Idle => _stats[3];
        /// <summary>
        /// waiting for I/O to complete
        /// </summary>
        public int Iowait => _stats[4];
        /// <summary>
        /// servicing interrupts
        /// </summary>
        public int Irq => _stats[5];
        /// <summary>
        /// servicing softirqs
        /// </summary>
        public int Softirq => _stats[6];

        private long Total =>
            _stats[0] +
            _stats[1] +
            _stats[2] +
            _stats[3] +
            _stats[4] +
            _stats[5] +
            _stats[6];
    }
}
