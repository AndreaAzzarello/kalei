using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

namespace SandboxSpike.Launcher;

public enum Isolation { None, AppContainer, Lpac }

public sealed class RunOptions
{
    public Isolation Isolation { get; init; } = Isolation.AppContainer;
    public required string Mode { get; init; }
    public required string WorkerDir { get; init; }
    public string? SourcePath { get; init; }
    public string? OutputPath { get; init; }
    public string[] ExtraArgs { get; init; } = [];
    public int TimeoutMs { get; init; } = 30_000;
    public ulong MemoryLimitBytes { get; init; } = 2UL * 1024 * 1024 * 1024; // Limits table: Worker memory 2 GB
    public bool LimitToOneProcess { get; init; } = true;
}

public sealed class RunResult
{
    public bool Started { get; init; }
    public int StartError { get; init; }
    public bool TimedOut { get; init; }
    public uint ExitCode { get; init; }
    public double HelloMs { get; init; }
    public double TotalMs { get; init; }
    public long PeakMemoryBytes { get; init; }
    public Dictionary<string, JsonElement>? Result { get; init; }

    /// <summary>True when the Worker started and reported a result (so a failure inside it is meaningful).</summary>
    public bool Reported => Result is not null;
    public bool WorkerOk => Result is not null && Result.TryGetValue("ok", out var ok) && ok.ValueKind == JsonValueKind.True;
    public string? Error => Result is not null && Result.TryGetValue("error", out var e) ? e.GetString() : null;
    public T? Get<T>(string key) => Result is not null && Result.TryGetValue(key, out var v) ? v.Deserialize<T>() : default;
}

/// <summary>A running Worker inside its Job Object. Disposing the job kills the Worker (kill-on-close).</summary>
public sealed class SandboxProcess : IDisposable
{
    public const string ProfileName = "Kalei.Spike.Worker";
    private IntPtr _job;
    private IntPtr _process;
    private IntPtr _thread;
    private readonly List<SafeFileHandle> _keepOpen = [];
    private Task<(string? hello, string? result, double helloMs)>? _reader;
    private readonly Stopwatch _clock = new();

    public int ProcessId { get; private set; }

    public static IntPtr EnsureProfileSid()
    {
        var hr = Native.CreateAppContainerProfile(ProfileName, "Kalei spike worker", "Throwaway spike profile, no capabilities", IntPtr.Zero, 0, out var sid);
        if (hr == 0) return sid;
        if (unchecked((uint)hr) == 0x800700B7) // ALREADY_EXISTS
        {
            Marshal.ThrowExceptionForHR(Native.DeriveAppContainerSidFromAppContainerName(ProfileName, out sid));
            return sid;
        }
        Marshal.ThrowExceptionForHR(hr);
        return IntPtr.Zero;
    }

    public static void DeleteProfile() => Native.DeleteAppContainerProfile(ProfileName);

    /// <summary>Start a Worker. Returns a result with Started == false and the Win32 error if the sandbox launch fails.</summary>
    public static SandboxProcess Start(RunOptions o, out int win32Error)
    {
        var p = new SandboxProcess();
        win32Error = p.StartCore(o);
        return p;
    }

    private int StartCore(RunOptions o)
    {
        var sa = new Native.SECURITY_ATTRIBUTES { nLength = Marshal.SizeOf<Native.SECURITY_ATTRIBUTES>(), bInheritHandle = 1 };
        if (!Native.CreatePipe(out var pipeRead, out var pipeWrite, ref sa, 0)) return Marshal.GetLastWin32Error();
        Native.SetHandleInformation(pipeRead, Native.HANDLE_FLAG_INHERIT, 0); // only the write end is inherited

        var inherited = new List<IntPtr> { pipeWrite };
        var args = new StringBuilder($"\"{Path.Combine(o.WorkerDir, "SandboxSpike.Worker.exe")}\" {o.Mode} --pipe {pipeWrite}");
        if (o.SourcePath is not null)
        {
            var h = File.OpenHandle(o.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            Native.SetHandleInformation(h.DangerousGetHandle(), Native.HANDLE_FLAG_INHERIT, Native.HANDLE_FLAG_INHERIT);
            _keepOpen.Add(h);
            inherited.Add(h.DangerousGetHandle());
            args.Append($" --in {h.DangerousGetHandle()}");
        }
        if (o.OutputPath is not null)
        {
            var h = File.OpenHandle(o.OutputPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
            Native.SetHandleInformation(h.DangerousGetHandle(), Native.HANDLE_FLAG_INHERIT, Native.HANDLE_FLAG_INHERIT);
            _keepOpen.Add(h);
            inherited.Add(h.DangerousGetHandle());
            args.Append($" --out {h.DangerousGetHandle()}");
        }
        foreach (var x in o.ExtraArgs) // "--key value": the value is quoted, it may contain spaces
        {
            var sp = x.IndexOf(' ');
            args.Append(' ').Append(sp < 0 ? x : $"{x[..sp]} \"{x[(sp + 1)..]}\"");
        }

        // Attribute list: handle list (+ AppContainer capabilities, + LPAC opt-out of ALL APPLICATION PACKAGES).
        var attrCount = 1 + (o.Isolation != Isolation.None ? 1 : 0) + (o.Isolation == Isolation.Lpac ? 1 : 0);
        var size = IntPtr.Zero;
        Native.InitializeProcThreadAttributeList(IntPtr.Zero, attrCount, 0, ref size);
        var list = Marshal.AllocHGlobal(size);
        var allocs = new List<IntPtr> { list };
        try
        {
            if (!Native.InitializeProcThreadAttributeList(list, attrCount, 0, ref size)) return Marshal.GetLastWin32Error();

            var handleArray = Marshal.AllocHGlobal(IntPtr.Size * inherited.Count);
            allocs.Add(handleArray);
            for (var i = 0; i < inherited.Count; i++) Marshal.WriteIntPtr(handleArray, i * IntPtr.Size, inherited[i]);
            if (!Native.UpdateProcThreadAttribute(list, 0, Native.PROC_THREAD_ATTRIBUTE_HANDLE_LIST, handleArray,
                    new IntPtr(IntPtr.Size * inherited.Count), IntPtr.Zero, IntPtr.Zero)) return Marshal.GetLastWin32Error();

            if (o.Isolation != Isolation.None)
            {
                var caps = new Native.SECURITY_CAPABILITIES { AppContainerSid = EnsureProfileSid(), Capabilities = IntPtr.Zero, CapabilityCount = 0 };
                var capsPtr = Marshal.AllocHGlobal(Marshal.SizeOf<Native.SECURITY_CAPABILITIES>());
                allocs.Add(capsPtr);
                Marshal.StructureToPtr(caps, capsPtr, false);
                if (!Native.UpdateProcThreadAttribute(list, 0, Native.PROC_THREAD_ATTRIBUTE_SECURITY_CAPABILITIES, capsPtr,
                        new IntPtr(Marshal.SizeOf<Native.SECURITY_CAPABILITIES>()), IntPtr.Zero, IntPtr.Zero)) return Marshal.GetLastWin32Error();
            }
            if (o.Isolation == Isolation.Lpac)
            {
                var policy = Marshal.AllocHGlobal(4);
                allocs.Add(policy);
                Marshal.WriteInt32(policy, (int)Native.PROCESS_CREATION_ALL_APPLICATION_PACKAGES_OPT_OUT);
                if (!Native.UpdateProcThreadAttribute(list, 0, Native.PROC_THREAD_ATTRIBUTE_ALL_APPLICATION_PACKAGES_POLICY, policy,
                        new IntPtr(4), IntPtr.Zero, IntPtr.Zero)) return Marshal.GetLastWin32Error();
            }

            var si = new Native.STARTUPINFOEX { lpAttributeList = list };
            si.StartupInfo.cb = Marshal.SizeOf<Native.STARTUPINFOEX>();
            _clock.Restart();
            if (!Native.CreateProcessW(null, args, IntPtr.Zero, IntPtr.Zero, true,
                    Native.EXTENDED_STARTUPINFO_PRESENT | Native.CREATE_SUSPENDED | Native.CREATE_NO_WINDOW,
                    IntPtr.Zero, o.WorkerDir, ref si, out var pi))
            {
                var err = Marshal.GetLastWin32Error();
                Native.CloseHandle(pipeWrite);
                Native.CloseHandle(pipeRead);
                return err;
            }
            _process = pi.hProcess;
            _thread = pi.hThread;
            ProcessId = pi.dwProcessId;

            // Job Object: memory cap, at most one process (no children), kill when the last handle closes.
            _job = Native.CreateJobObjectW(IntPtr.Zero, null);
            var info = new Native.JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
            info.BasicLimitInformation.LimitFlags = Native.JOB_OBJECT_LIMIT_PROCESS_MEMORY | Native.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
                | (o.LimitToOneProcess ? Native.JOB_OBJECT_LIMIT_ACTIVE_PROCESS : 0);
            info.BasicLimitInformation.ActiveProcessLimit = 1;
            info.ProcessMemoryLimit = new UIntPtr(o.MemoryLimitBytes);
            if (!Native.SetInformationJobObject(_job, Native.JobObjectExtendedLimitInformation, ref info,
                    Marshal.SizeOf<Native.JOBOBJECT_EXTENDED_LIMIT_INFORMATION>()) || !Native.AssignProcessToJobObject(_job, _process))
            {
                var err = Marshal.GetLastWin32Error();
                Native.TerminateProcess(_process, 1);
                return err;
            }

            // Our copy of the write end must be closed, otherwise the reader never sees the end of the stream.
            Native.CloseHandle(pipeWrite);
            var reader = new StreamReader(new FileStream(new SafeFileHandle(pipeRead, true), FileAccess.Read, 1, false));
            var clock = _clock;
            _reader = Task.Run(() =>
            {
                string? hello = null, result = null;
                var helloMs = 0.0;
                while (reader.ReadLine() is { } line)
                {
                    if (line == "HELLO") { hello = line; helloMs = clock.Elapsed.TotalMilliseconds; }
                    else if (line.StartsWith("RESULT ")) result = line[7..];
                }
                return (hello, result, helloMs);
            });
            Native.ResumeThread(_thread);
            return 0;
        }
        finally
        {
            Native.DeleteProcThreadAttributeList(list);
            foreach (var a in allocs) Marshal.FreeHGlobal(a);
        }
    }

    /// <summary>Wait for the Worker; on timeout the whole job is terminated.</summary>
    public RunResult Wait(int timeoutMs)
    {
        var timedOut = Native.WaitForSingleObject(_process, (uint)timeoutMs) == Native.WAIT_TIMEOUT;
        if (timedOut) Native.TerminateJobObject(_job, 1);
        Native.WaitForSingleObject(_process, 5000);
        var total = _clock.Elapsed.TotalMilliseconds;
        Native.GetExitCodeProcess(_process, out var exit);
        Native.QueryInformationJobObject(_job, Native.JobObjectExtendedLimitInformation, out var info,
            Marshal.SizeOf<Native.JOBOBJECT_EXTENDED_LIMIT_INFORMATION>(), IntPtr.Zero);
        var (_, result, helloMs) = _reader!.GetAwaiter().GetResult();
        Dictionary<string, JsonElement>? parsed = null;
        if (result is not null) parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(result);
        return new RunResult
        {
            Started = true, TimedOut = timedOut, ExitCode = exit, HelloMs = helloMs, TotalMs = total,
            PeakMemoryBytes = (long)info.PeakProcessMemoryUsed.ToUInt64(), Result = parsed,
        };
    }

    public static RunResult Run(RunOptions o)
    {
        using var p = Start(o, out var err);
        if (err != 0) return new RunResult { Started = false, StartError = err };
        return p.Wait(o.TimeoutMs);
    }

    public void Dispose()
    {
        if (_job != IntPtr.Zero) { Native.CloseHandle(_job); _job = IntPtr.Zero; } // kill-on-close fires here
        if (_thread != IntPtr.Zero) { Native.CloseHandle(_thread); _thread = IntPtr.Zero; }
        if (_process != IntPtr.Zero) { Native.CloseHandle(_process); _process = IntPtr.Zero; }
        foreach (var h in _keepOpen) h.Dispose();
        _keepOpen.Clear();
    }
}
