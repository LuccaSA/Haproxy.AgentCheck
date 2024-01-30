using System.Globalization;
using System.Runtime.Versioning;

namespace Lucca.Infra.Haproxy.AgentCheck.Metrics;

[SupportedOSPlatform("linux")]
internal sealed class LinuxStateCollector(State state) : IStateCollector
{
    private ProcStat _lastStat = ProcStat.Empty;

    public void Collect()
    {
        using var reader = new StreamReader("/proc/stat");
        var stat = ProcStat.FromLine(reader.ReadLine());

        if (_lastStat != ProcStat.Empty)
        {
            state.UpdateState(new SystemState { CpuPercent = _lastStat.AverageCpuWith(stat) });
        }

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

    internal static ProcStat FromLine(string? firstProcStatLine)
    {
        if (firstProcStatLine == null) throw new ArgumentNullException(nameof(firstProcStatLine), "/proc/stat is returning invalid data");

        if (!firstProcStatLine.StartsWith("cpu ", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("/proc/stat is returning invalid first line", nameof(firstProcStatLine));

        var span = firstProcStatLine.AsSpan();
        span = span[3..].TrimStart(' '); // "cpu"
        var stats = new int[7];

        for (int i = 0; i < 7; i++)
        {
            var nextSpace = span.IndexOf(' ');
            if (nextSpace == -1) throw new ArgumentException("/proc/stat structure is invalid", nameof(firstProcStatLine));
            stats[i] = int.Parse(span[..nextSpace], provider: CultureInfo.InvariantCulture);
            span = span[(nextSpace + 1)..];
        }

        return new ProcStat(stats);
    }

    internal double AverageCpuWith(ProcStat with) => 100d * (1 - 1d * (Idle - with.Idle) / (Total - with.Total));

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
