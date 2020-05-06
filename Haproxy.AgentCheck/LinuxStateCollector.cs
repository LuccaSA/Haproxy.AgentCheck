using System;
using System.IO;

namespace Haproxy.AgentCheck
{
    public class LinuxStateCollector : IStateCollector
    {
        private readonly State _state;
        private ProcStat _lastStat;

        public LinuxStateCollector(State state)
        {
            _state = state;
        }

        public void Collect()
        {
            using var reader = new StreamReader("/proc/stat");
            var stat = ProcStat.FromLine(reader.ReadLine());

            if (_lastStat != null)
                _state.CpuPercent = _lastStat.AverageCpuWith(stat);

            _lastStat = stat;
        }
    }

    internal class ProcStat
    {
        private readonly int[] _stats;

        private ProcStat(int[] stats)
        {
            _stats = stats;
        }

        internal static ProcStat FromLine(string firstProcStatLine)
        {
            if (firstProcStatLine?.StartsWith("cpu ") != true)
                throw new ArgumentNullException(nameof(firstProcStatLine), "/proc/stat is returning invalid data");
 
            var span = firstProcStatLine.AsSpan();
            span = span.Slice(5); // "cpu  "
            var stats = new int[7];

            for (int i = 0; i < 7; i++)
            {
                var nextSpace = span.IndexOf(' ');
                stats[i] = int.Parse(span.Slice(0, nextSpace));
                span = span.Slice(nextSpace + 1);
            }

            return new ProcStat(stats);
        }

        internal int AverageCpuWith(ProcStat with)
        {
            if (with == null)
                throw new ArgumentNullException(nameof(with));
            return 100 - (int) Math.Floor((Idle - with.Idle) * 100 / (double) (Total - with.Total));
        }

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
