using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Kalei.Core.Ipc;
using Kalei.Core.Logging;
using Kalei.Core.Platform;
using Kalei.Platform.Windows.Sandbox;

namespace Kalei.SandboxCheck;

/// <summary>
/// The sandbox proofs of ticket 07b for a machine without the .NET SDK (the reference VM): the same checks as
/// SandboxLauncherTests, each with its control outside the sandbox. A "must be blocked" proof is PASS only if the control worked
/// and the sandbox blocked; INCONCLUSIVE if the control did not work; FAIL if the sandbox let it through.
/// </summary>
internal static class Proofs
{
    private static readonly string s_tools = AppContext.BaseDirectory.TrimEnd('\\');
    private static readonly string s_fakeWorker = Path.Combine(AppContext.BaseDirectory, "Kalei.FakeWorker.exe");
    private static readonly ProbeMessage s_probe = new() { Source = 1 };
    private static string s_profile = "";
    private static SandboxSetup s_setup = null!;

    public static async Task<List<ProofResult>> RunAllAsync(string profile)
    {
        s_profile = profile;
        s_setup = new SandboxSetup(profile, s_tools);
        var status = s_setup.Prepare();
        if (status != SandboxSetupStatus.Ready)
        {
            return [new ProofResult("setup", "first-run set-up", "FAIL", "", status.ToString(), "")];
        }

        using var scratch = new Scratch();
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port.ToString(CultureInfo.InvariantCulture);

        List<ProofResult> results = [];
        results.Add(await BlockedAsync("net-tcp-local", "TCP to a listener on 127.0.0.1", false, "tcp", "127.0.0.1", port));
        results.Add(await BlockedAsync("net-tcp-internet", "TCP connect to 1.1.1.1:443 (S5)", true, "tcp", "1.1.1.1", "443"));
        results.Add(await BlockedAsync("net-udp-dns", "UDP DNS query to 1.1.1.1:53", true, "udp-dns", "1.1.1.1"));
        results.Add(await BlockedAsync("net-dns-lookup", "DNS lookup of a host name (S5)", true, "dns"));
        results.Add(await BlockedAsync("fs-read-path", "read a file by path", false, "read-file", Hex(scratch.Canary)));
        results.Add(await BlockedAsync("fs-list-profile", "list the user profile folder", false, "list-dir", Hex(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))));
        results.Add(await BlockedAsync("fs-write-temp", "write in the temp folder", false, "write-dir", Hex(Path.GetTempPath())));
        results.Add(await BlockedAsync("fs-write-install", "write in the folder of the Worker", false, "write-dir", Hex(s_tools)));
        results.Add(await HandleAsync(scratch.Canary, listed: true));
        results.Add(await HandleAsync(scratch.Canary, listed: false));
        results.Add(await BlockedAsync("child-process", "start a child process", false, "child"));
        results.Add(await BlockedAsync("environment", "user environment variables", false, "env"));
        results.Add(await SessionAsync("memory-cap", "256 MB cap ends an allocating Worker with E_MEM", "AllocateForever",
            new RequestOptions { Timeout = TimeSpan.FromSeconds(60), MemoryLimitBytes = 256L * 1024 * 1024 }, null, TimeSpan.FromSeconds(30), ErrorCode.Mem, null));
        results.Add(await CpuCapAsync());
        results.Add(await SessionAsync("time-cap", "2 s request timeout ends a spinning Worker with E_TIME", "SpinAfterHandshake",
            new RequestOptions { Timeout = TimeSpan.FromSeconds(2) }, null, TimeSpan.FromSeconds(10), ErrorCode.Time, ProtocolReason.Timeout));
        results.Add(await SessionAsync("w16-hang", "W16: no heartbeat, killed within 12 s of the start", "StallAfterHello",
            new RequestOptions { Timeout = TimeSpan.FromMinutes(5) }, null, TimeSpan.FromSeconds(12), ErrorCode.Time, ProtocolReason.Heartbeat));
        results.Add(await SessionAsync("w16-cancel", "W16: Cancel ends a Worker that ignores it (3 s grace)", "Heartbeats",
            new RequestOptions { Timeout = TimeSpan.FromMinutes(5) }, TimeSpan.FromSeconds(1.5), TimeSpan.FromSeconds(8), ErrorCode.Cancelled, null));
        results.Add(await KillOnCloseAsync("wait"));
        results.Add(await KillOnCloseAsync("exit"));
        results.Add(await SetupMissingAsync());
        return results;
    }

    // ----------------------------------------------------------------------------------------------------- probes in and out

    private static string Hex(string text) => Convert.ToHexString(Encoding.UTF8.GetBytes(text));

    private static async Task<string> ReadLineAsync(Stream stream)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        try
        {
            return await reader.ReadLineAsync(timeout.Token) ?? "<the Worker wrote nothing>";
        }
        catch (OperationCanceledException)
        {
            return "<no answer in 30 s>";
        }
    }

    private static async Task<string> InsideAsync(string probe, string[] extra, WorkerStart? start = null)
    {
        try
        {
            var launcher = new WindowsSandboxLauncher(s_setup, s_fakeWorker, [$"probe:{probe}", .. extra]);
            using var worker = launcher.Start(start ?? new WorkerStart());
            var line = await ReadLineAsync(worker.FromWorker);
            worker.Kill();
            await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10)); // its inherited handles close when it is gone
            return line;
        }
        catch (SandboxStartException e)
        {
            return $"start failed: {e.Failure} {e.OsError}";
        }
    }

    private static async Task<string> OutsideAsync(string probe, string[] extra)
    {
        using var toWorker = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
        using var fromWorker = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
        var info = new ProcessStartInfo(s_fakeWorker) { UseShellExecute = false, CreateNoWindow = true };
        info.ArgumentList.Add($"probe:{probe}");
        foreach (var e in extra)
        {
            info.ArgumentList.Add(e);
        }

        info.ArgumentList.Add(toWorker.GetClientHandleAsString());
        info.ArgumentList.Add(fromWorker.GetClientHandleAsString());
        using var process = Process.Start(info) ?? throw new InvalidOperationException("no process");
        toWorker.DisposeLocalCopyOfClientHandle();
        fromWorker.DisposeLocalCopyOfClientHandle();
        var line = await ReadLineAsync(fromWorker);
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // already ended
        }

        process.WaitForExit(10_000);
        return line;
    }

    private static async Task<ProofResult> BlockedAsync(string id, string description, bool needsInternet, string probe, params string[] extra)
    {
        var control = await OutsideAsync(probe, extra);
        if (!control.StartsWith("ok", StringComparison.Ordinal))
        {
            return new ProofResult(id, description, "INCONCLUSIVE", control, "", needsInternet ? "no Internet for the control outside the sandbox" : "the control outside the sandbox did not work");
        }

        var inside = await InsideAsync(probe, extra);
        return new ProofResult(id, description, inside.StartsWith("blocked", StringComparison.Ordinal) ? "PASS" : "FAIL", control, inside, "");
    }

    private static async Task<ProofResult> HandleAsync(string file, bool listed)
    {
        using var handle = File.OpenHandle(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        var value = handle.DangerousGetHandle().ToInt64().ToString(CultureInfo.InvariantCulture);
        var inside = await InsideAsync("read-handle", [value], listed ? new WorkerStart { InheritedHandles = [handle] } : new WorkerStart());
        var pass = listed ? inside == "ok HANDLE-CONTENT" : inside.StartsWith("blocked", StringComparison.Ordinal);
        return new ProofResult(listed ? "handle-listed" : "handle-unlisted",
            listed ? "a listed handle is readable and seekable (the file by path is denied)" : "a handle that is not listed is not inherited",
            pass ? "PASS" : "FAIL", "", inside, "");
    }

    // ----------------------------------------------------------------------------------------------------- limits and ending

    private sealed class Catalog : IModuleCatalog
    {
        public bool HasModule(string id, string version) => id == "images" && version == "1.0.0";

        public bool HasFormat(string id) => id is "png" or "webp";
    }

    private sealed class NoLog : IKaleiLog
    {
        public void Write(LogEvent logEvent)
        {
        }
    }

    private sealed class Recording(ISandboxLauncher inner) : ISandboxLauncher
    {
        public IWorkerProcess? Last { get; private set; }

        public IWorkerProcess Start(WorkerStart start) => Last = inner.Start(start);
    }

    private static async Task<ProofResult> SessionAsync(string id, string description, string behaviour, RequestOptions options, TimeSpan? cancelAfter,
        TimeSpan within, ErrorCode expected, ProtocolReason? expectedReason)
    {
        var launcher = new Recording(new WindowsSandboxLauncher(s_setup, s_fakeWorker, behaviour));
        var session = new WorkerSession(launcher, new Catalog(), new NoLog(), WatchdogTimes.Default);
        using var cancel = new CancellationTokenSource();
        if (cancelAfter is { } after)
        {
            cancel.CancelAfter(after);
        }

        var clock = Stopwatch.StartNew();
        var outcome = await session.RunAsync(s_probe, options, null, cancel.Token);
        clock.Stop();
        var ended = launcher.Last is { } worker && worker.WaitForExitAsync().IsCompleted;
        var actual = $"{outcome.Error?.ToString() ?? "no error"}{(outcome.Reason is { } r ? " (" + r + ")" : "")} after {clock.Elapsed.TotalSeconds:F1} s, Worker ended: {ended}";
        var pass = outcome.Error == expected && (expectedReason is null || outcome.Reason == expectedReason) && clock.Elapsed < within && ended;
        return new ProofResult(id, description, pass ? "PASS" : "FAIL", "", actual, "");
    }

    private static async Task<ProofResult> CpuCapAsync()
    {
        using var worker = new WindowsSandboxLauncher(s_setup, s_fakeWorker, "probe:spin").Start(new WorkerStart { CpuTimeLimit = TimeSpan.FromSeconds(1) });
        try
        {
            await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(20));
        }
        catch (TimeoutException)
        {
            return new ProofResult("cpu-cap", "1 s CPU cap ends a spinning Worker", "FAIL", "", "still running after 20 s", "");
        }

        var cause = worker.EndCause;
        return new ProofResult("cpu-cap", "1 s CPU cap ends a spinning Worker", cause == WorkerEndCause.TimeLimit ? "PASS" : "FAIL", "", cause.ToString(), "");
    }

    private static async Task<ProofResult> KillOnCloseAsync(string mode)
    {
        var id = "kill-on-close-" + mode;
        var description = mode == "wait" ? "the Worker dies when its launcher process is killed" : "the Worker dies when its launcher process ends normally";
        var info = new ProcessStartInfo(Environment.ProcessPath!, ["host", s_fakeWorker, s_profile, s_tools, mode])
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var host = Process.Start(info) ?? throw new InvalidOperationException("no host");
        try
        {
            var line = await host.StandardOutput.ReadLineAsync();
            if (line is null || !line.StartsWith("ok ", StringComparison.Ordinal) || !int.TryParse(line.AsSpan(3), CultureInfo.InvariantCulture, out var pid))
            {
                return new ProofResult(id, description, "FAIL", "", $"the helper said: {line ?? "nothing"}", "");
            }

            if (mode == "wait")
            {
                host.Kill();
            }

            await host.WaitForExitAsync();
            var clock = Stopwatch.StartNew();
            while (IsRunning(pid) && clock.Elapsed < TimeSpan.FromSeconds(5))
            {
                await Task.Delay(50);
            }

            var gone = !IsRunning(pid);
            return new ProofResult(id, description, gone ? "PASS" : "FAIL", "", gone ? $"gone {clock.ElapsedMilliseconds} ms after its launcher" : "the Worker outlived its launcher", "");
        }
        finally
        {
            if (!host.HasExited)
            {
                host.Kill();
            }
        }
    }

    private static bool IsRunning(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static async Task<ProofResult> SetupMissingAsync()
    {
        using var folder = new Scratch();
        var someoneElse = new System.Security.Principal.SecurityIdentifier("S-1-1-0");
        var setup = new SandboxSetup(s_profile + ".nosetup", folder.Folder, someoneElse); // no grant and "not the user's": set-up must refuse
        var launcher = new Recording(new WindowsSandboxLauncher(setup, s_fakeWorker, "Normal"));
        try
        {
            var outcome = await new WorkerSession(launcher, new Catalog(), new NoLog(), WatchdogTimes.Default)
                .RunAsync(s_probe, new RequestOptions { Timeout = TimeSpan.FromSeconds(5) }, null, CancellationToken.None);
            var pass = outcome.Error == ErrorCode.Sandbox && launcher.Last is null;
            return new ProofResult("setup-missing", "without the set-up nothing starts and the answer is E_SANDBOX", pass ? "PASS" : "FAIL", "",
                $"{outcome.Error}, Worker created: {launcher.Last is not null}", "");
        }
        finally
        {
            setup.Remove();
        }
    }

    /// <summary>A folder with a canary file, removed afterwards.</summary>
    private sealed class Scratch : IDisposable
    {
        public Scratch()
        {
            Folder = Path.Combine(Path.GetTempPath(), "kalei-07b-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Folder);
            Canary = Path.Combine(Folder, "canary.txt");
            File.WriteAllText(Canary, "KALEI-HANDLE-CONTENT");
        }

        public string Folder { get; }

        public string Canary { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Folder, recursive: true);
            }
            catch (IOException)
            {
                // a scanner may hold the file for a moment
            }
        }
    }
}

/// <param name="Id">Short name of the proof.</param>
/// <param name="Description">What is proved.</param>
/// <param name="Verdict">PASS, FAIL, or INCONCLUSIVE (the control outside the sandbox did not work, so nothing was proved).</param>
/// <param name="Control">The result outside the sandbox (only error kinds, never a path).</param>
/// <param name="Inside">The result inside the sandbox.</param>
/// <param name="Note">Why a proof was inconclusive.</param>
internal sealed record ProofResult(string Id, string Description, string Verdict, string Control, string Inside, string Note);
