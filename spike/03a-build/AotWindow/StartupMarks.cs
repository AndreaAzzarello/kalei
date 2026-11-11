using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace AotWindow;

/// <summary>
/// Phase markers for the cold-start breakdown (ticket 03c). A mark is a name and the UTC time; nothing heavy runs before the
/// second frame (no Process object, no assembly enumeration): <see cref="Report"/> is called after the "frame2" mark.
/// t0 is the kernel creation time of the process, read at the end, as in the W7 measure of #47.
/// "runtime" is the first managed code of the app (module initializer): it includes the apphost, hostfxr and hostpolicy
/// work and the start of the CLR, which cannot be separated without event tracing.
/// No path, user name or computer name is ever written.
/// </summary>
internal static class StartupMarks
{
    private static readonly string[] Names = new string[24];
    private static readonly long[] Ticks = new long[24];
    private static int _count;

    [ModuleInitializer]
    internal static void Init() => Mark("runtime");

    public static void Mark(string name)
    {
        var i = Interlocked.Increment(ref _count) - 1;
        if (i >= Names.Length) return;
        Names[i] = name;
        Ticks[i] = DateTime.UtcNow.Ticks;
    }

    [DllImport("kernel32.dll")]
    private static extern bool GetProcessIoCounters(IntPtr process, out IoCounters counters);

    [StructLayout(LayoutKind.Sequential)]
    private struct IoCounters
    {
        public ulong ReadOperationCount, WriteOperationCount, OtherOperationCount, ReadTransferCount, WriteTransferCount, OtherTransferCount;
    }

    /// <summary>One JSON line: marks in ms from t0, loaded assemblies (names only), CPU, bytes read, peak working set.</summary>
    public static string Report(string variant)
    {
        var inv = CultureInfo.InvariantCulture;
        using var proc = Process.GetCurrentProcess();
        var t0 = proc.StartTime.ToUniversalTime().Ticks;
        var sb = new StringBuilder(2048);
        sb.Append("{\"variant\":\"").Append(variant).Append("\",\"marks\":{");
        var n = Math.Min(_count, Names.Length);
        for (var i = 0; i < n; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append('"').Append(Names[i]).Append("\":").Append(((Ticks[i] - t0) / 10000.0).ToString("F1", inv));
        }
        sb.Append("},\"assemblies\":[");
        var asms = AppDomain.CurrentDomain.GetAssemblies();
        for (var i = 0; i < asms.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append('"').Append(asms[i].GetName().Name).Append('"');
        }
        sb.Append("],\"assemblyCount\":").Append(asms.Length.ToString(inv));
        sb.Append(",\"cpuUserMs\":").Append(proc.UserProcessorTime.TotalMilliseconds.ToString("F0", inv));
        sb.Append(",\"cpuKernelMs\":").Append(proc.PrivilegedProcessorTime.TotalMilliseconds.ToString("F0", inv));
        sb.Append(",\"peakWorkingSetMB\":").Append((proc.PeakWorkingSet64 / 1048576.0).ToString("F1", inv));
        if (GetProcessIoCounters(proc.Handle, out var io))
            sb.Append(",\"readBytes\":").Append(io.ReadTransferCount.ToString(inv)).Append(",\"readOps\":").Append(io.ReadOperationCount.ToString(inv));
        sb.Append(",\"cpus\":").Append(Environment.ProcessorCount.ToString(inv));
        sb.Append('}');
        return sb.ToString();
    }
}
