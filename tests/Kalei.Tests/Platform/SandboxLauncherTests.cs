using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Text;
using Kalei.Core.Ipc;
using Kalei.Core.Logging;
using Kalei.Core.Platform;
using Kalei.Platform.Windows.Sandbox;
using Kalei.Tests.Ipc;
using Xunit;

namespace Kalei.Tests.Platform;

/// <summary>
/// The real Workers started by the real Windows launcher: AppContainer, Job Object, inherited anonymous pipes (ticket 07b).
/// Every "must be blocked" test first runs the same probe outside the sandbox, and fails as INCONCLUSIVE if that control does not
/// succeed: a block is only proved by something that works without the sandbox.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class SandboxLauncherTests(SandboxFixture sandbox) : IClassFixture<SandboxFixture>
{
    private sealed class FakeCatalog : IModuleCatalog
    {
        public bool HasModule(string id, string version) => id == "images" && version == "1.0.0";

        public bool HasFormat(string id) => id is "png" or "webp";
    }

    private sealed class RecordingLauncher(ISandboxLauncher inner) : ISandboxLauncher
    {
        public IWorkerProcess? Last { get; private set; }

        public IWorkerProcess Start(WorkerStart start) => Last = inner.Start(start);
    }

    private static string FakeWorkerExe => Path.Combine(AppContext.BaseDirectory, "Kalei.FakeWorker.exe");

    private static string Hex(string text) => Convert.ToHexString(Encoding.UTF8.GetBytes(text));

    private static readonly ProbeMessage s_probe = new() { Source = 1 };

    // --- helpers -------------------------------------------------------------------------------------------------------------

    private static async Task<string> ReadLineAsync(IWorkerProcess worker)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var reader = new StreamReader(worker.FromWorker, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadLineAsync(timeout.Token) ?? "<the Worker wrote nothing>";
    }

    private async Task<string> InSandboxAsync(string probe, WorkerStart? start = null, params string[] extra)
    {
        using var worker = sandbox.Launcher(FakeWorkerExe, [$"probe:{probe}", .. extra]).Start(start ?? new WorkerStart());
        var line = await ReadLineAsync(worker);
        worker.Kill();
        await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10)); // its inherited handles are closed when it is gone
        return line;
    }

    private static async Task<string> OutsideAsync(string probe, params string[] extra)
    {
        using var worker = new ProcessLauncher(FakeWorkerExe, [$"probe:{probe}", .. extra]).Start(new WorkerStart());
        var line = await ReadLineAsync(worker);
        worker.Kill();
        await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
        return line;
    }

    private async Task AssertBlockedAsync(string probe, params string[] extra)
    {
        var control = await OutsideAsync(probe, extra);
        if (!control.StartsWith("ok", StringComparison.Ordinal))
        {
            Assert.Fail($"INCONCLUSIVE: the control outside the sandbox did not succeed ({control}).");
        }

        Assert.StartsWith("blocked", await InSandboxAsync(probe, null, extra), StringComparison.Ordinal);
    }

    /// <summary>The control needs the Internet. Without it the test is skipped (not passed): nothing was proved.</summary>
    private async Task AssertBlockedNeedingInternetAsync(string probe, params string[] extra)
    {
        var control = await OutsideAsync(probe, extra);
        if (!control.StartsWith("ok", StringComparison.Ordinal))
        {
            Assert.Skip($"INCONCLUSIVE: no Internet for the control outside the sandbox ({control}).");
        }

        Assert.StartsWith("blocked", await InSandboxAsync(probe, null, extra), StringComparison.Ordinal);
    }

    // --- the real Worker -----------------------------------------------------------------------------------------------------

    [Fact]
    public async Task TheRealWorker_InTheSandbox_SendsItsHello_AndAnswersEFormat()
    {
        var launcher = sandbox.Launcher(BuiltinWorkerTests.BuiltinExe);
        var log = new BuiltinWorkerTests.NoLog();
        var session = new WorkerSession(launcher, new BuiltinWorkerTests.BuiltinCatalog(), log, WatchdogTimes.Default);

        var outcome = await session.RunAsync(s_probe, new RequestOptions { Timeout = TimeSpan.FromSeconds(30) }, null, TestContext.Current.CancellationToken);

        Assert.Empty(log.Events);
        Assert.Equal(ErrorCode.Format, outcome.Error);
        Assert.Null(outcome.Reason);
    }

    // --- network (S5) --------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Tcp_ToALocalListener_IsBlocked()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port.ToString(System.Globalization.CultureInfo.InvariantCulture);

        await AssertBlockedAsync("tcp", "127.0.0.1", port);
    }

    [Fact]
    public Task Tcp_ToTheInternet_IsBlocked() => AssertBlockedNeedingInternetAsync("tcp", "1.1.1.1", "443");

    [Fact]
    public Task Udp_DnsQuery_IsBlocked() => AssertBlockedNeedingInternetAsync("udp-dns", "1.1.1.1");

    [Fact]
    public Task Dns_Lookup_IsBlocked() => AssertBlockedNeedingInternetAsync("dns");

    // --- files, handles, processes, environment ------------------------------------------------------------------------------

    [Fact]
    public async Task ReadingAFileByPath_IsDenied()
    {
        using var folder = new TempFolder();
        File.WriteAllText(folder.Combine("canary.txt"), "canary");

        await AssertBlockedAsync("read-file", Hex(folder.Combine("canary.txt")));
    }

    [Fact]
    public Task ListingTheUserProfile_IsDenied() => AssertBlockedAsync("list-dir", Hex(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));

    [Fact]
    public Task WritingToTheTempFolder_IsDenied() => AssertBlockedAsync("write-dir", Hex(Path.GetTempPath()));

    [Fact]
    public Task WritingToTheInstallFolder_IsDenied() => AssertBlockedAsync("write-dir", Hex(AppContext.BaseDirectory));

    [Fact]
    public async Task AListedHandle_IsReadableAndSeekable()
    {
        using var folder = new TempFolder();
        File.WriteAllText(folder.Combine("source.bin"), "KALEI-HANDLE-CONTENT");
        using var handle = File.OpenHandle(folder.Combine("source.bin"), FileMode.Open, FileAccess.Read, FileShare.Read);

        var result = await InSandboxAsync("read-handle", new WorkerStart { InheritedHandles = [handle] }, handle.DangerousGetHandle().ToInt64().ToString(System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal("ok HANDLE-CONTENT", result); // the file by path is denied (test above): this came through the handle
    }

    [Fact]
    public async Task AHandleThatIsNotListed_IsNotInherited()
    {
        using var folder = new TempFolder();
        File.WriteAllText(folder.Combine("source.bin"), "KALEI-HANDLE-CONTENT");
        using var handle = File.OpenHandle(folder.Combine("source.bin"), FileMode.Open, FileAccess.Read, FileShare.Read);

        var result = await InSandboxAsync("read-handle", new WorkerStart(), handle.DangerousGetHandle().ToInt64().ToString(System.Globalization.CultureInfo.InvariantCulture));

        Assert.StartsWith("blocked", result, StringComparison.Ordinal);
    }

    [Fact]
    public Task StartingAChildProcess_IsRefused() => AssertBlockedAsync("child");

    [Fact]
    public Task TheWorker_HasNothingOfTheUsersEnvironment() => AssertBlockedAsync("env");

    // --- limits and ending (S1, S2, W16) -------------------------------------------------------------------------------------

    [Fact]
    public async Task MemoryCap_EndsTheWorker_WithEMem()
    {
        var session = new WorkerSession(sandbox.Launcher(FakeWorkerExe, "AllocateForever"), new FakeCatalog(), new BuiltinWorkerTests.NoLog(), WatchdogTimes.Default);
        var options = new RequestOptions { Timeout = TimeSpan.FromSeconds(60), MemoryLimitBytes = 256L * 1024 * 1024 };

        var outcome = await session.RunAsync(s_probe, options, null, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCode.Mem, outcome.Error);
    }

    [Fact]
    public async Task CpuTimeCap_EndsTheWorker_AndSaysWhy()
    {
        using var worker = sandbox.Launcher(FakeWorkerExe, "probe:spin").Start(new WorkerStart { CpuTimeLimit = TimeSpan.FromSeconds(1) });

        await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken);

        Assert.Equal(WorkerEndCause.TimeLimit, worker.EndCause);
    }

    [Fact]
    public async Task RequestTimeout_EndsASpinningWorker_WithETime()
    {
        var launcher = new RecordingLauncher(sandbox.Launcher(FakeWorkerExe, "SpinAfterHandshake"));
        var session = new WorkerSession(launcher, new FakeCatalog(), new BuiltinWorkerTests.NoLog(), WatchdogTimes.Default);
        var clock = Stopwatch.StartNew();

        var outcome = await session.RunAsync(s_probe, new RequestOptions { Timeout = TimeSpan.FromSeconds(2) }, null, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCode.Time, outcome.Error);
        Assert.Equal(ProtocolReason.Timeout, outcome.Reason);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(10), $"took {clock.Elapsed}");
        Assert.True(launcher.Last!.WaitForExitAsync().IsCompleted);
    }

    [Fact]
    public async Task W16_AWorkerThatSendsNoHeartbeat_IsKilledWithinTenSeconds()
    {
        var launcher = new RecordingLauncher(sandbox.Launcher(FakeWorkerExe, "StallAfterHello"));
        var session = new WorkerSession(launcher, new FakeCatalog(), new BuiltinWorkerTests.NoLog(), WatchdogTimes.Default);
        var clock = Stopwatch.StartNew();

        var outcome = await session.RunAsync(s_probe, new RequestOptions { Timeout = TimeSpan.FromMinutes(5) }, null, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCode.Time, outcome.Error);
        Assert.Equal(ProtocolReason.Heartbeat, outcome.Reason);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(12), $"took {clock.Elapsed}"); // 10 s without a frame, plus the start of the Worker
        Assert.True(launcher.Last!.WaitForExitAsync().IsCompleted);
    }

    [Fact]
    public async Task W16_Cancel_EndsAWorkerThatIgnoresIt_AfterTheGracePeriod()
    {
        var launcher = new RecordingLauncher(sandbox.Launcher(FakeWorkerExe, "Heartbeats"));
        var session = new WorkerSession(launcher, new FakeCatalog(), new BuiltinWorkerTests.NoLog(), WatchdogTimes.Default);
        using var cancel = new CancellationTokenSource();
        cancel.CancelAfter(TimeSpan.FromSeconds(1.5));
        var clock = Stopwatch.StartNew();

        var outcome = await session.RunAsync(s_probe, new RequestOptions { Timeout = TimeSpan.FromMinutes(5) }, null, cancel.Token);

        Assert.Equal(ErrorCode.Cancelled, outcome.Error);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(8), $"took {clock.Elapsed}");
        Assert.True(launcher.Last!.WaitForExitAsync().IsCompleted);
    }

    [Theory]
    [InlineData("wait")] // the launcher's process is killed
    [InlineData("exit")] // the launcher's process ends normally
    public async Task TheWorker_DiesWithItsLauncher(string mode)
    {
        var hostExe = Path.Combine(AppContext.BaseDirectory, "Kalei.SandboxCheck.exe");
        using var host = Process.Start(new ProcessStartInfo(hostExe, ["host", FakeWorkerExe, sandbox.Setup.ProfileName, AppContext.BaseDirectory.TrimEnd('\\'), mode])
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        })!;
        try
        {
            var line = await host.StandardOutput.ReadLineAsync(TestContext.Current.CancellationToken);
            Assert.StartsWith("ok ", line, StringComparison.Ordinal);
            Assert.NotNull(line);
            var pid = int.Parse(line["ok ".Length..], System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(IsRunning(pid), "the Worker should be running while its launcher is");

            if (mode == "wait")
            {
                host.Kill();
            }

            await host.WaitForExitAsync(TestContext.Current.CancellationToken);
            var gone = SpinWait.SpinUntil(() => !IsRunning(pid), TimeSpan.FromSeconds(5));
            Assert.True(gone, "the Worker outlived its launcher");
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

    // --- never unsandboxed (S1, S5) ------------------------------------------------------------------------------------------

    [Fact]
    public async Task AWorkerThatCannotBeStarted_IsESandbox_AndIsLogged()
    {
        var launcher = sandbox.Launcher(Path.Combine(AppContext.BaseDirectory, "no-such-worker.exe"));
        var log = new BuiltinWorkerTests.NoLog();
        var session = new WorkerSession(launcher, new FakeCatalog(), log, WatchdogTimes.Default);

        var outcome = await session.RunAsync(s_probe, new RequestOptions { Timeout = TimeSpan.FromSeconds(5) }, null, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCode.Sandbox, outcome.Error);
        var logged = Assert.Single(log.Events);
        Assert.Equal(LogEventCode.SandboxStart, logged.Code);
        Assert.Equal(SandboxFailure.Process, logged.Sandbox);
    }

    [Fact]
    public async Task WhenTheSetUpIsMissing_NothingStarts_AndTheAnswerIsESandbox()
    {
        using var folder = new TempFolder(); // no grant for the sandbox, and not "owned" by the user we pretend to be
        var setup = new SandboxSetup("Kalei.Test." + Guid.NewGuid().ToString("N")[..12], folder.Path, new System.Security.Principal.SecurityIdentifier("S-1-1-0"));
        var launcher = new RecordingLauncher(new WindowsSandboxLauncher(setup, FakeWorkerExe, "Normal"));
        var log = new BuiltinWorkerTests.NoLog();
        try
        {
            var session = new WorkerSession(launcher, new FakeCatalog(), log, WatchdogTimes.Default);

            var outcome = await session.RunAsync(s_probe, new RequestOptions { Timeout = TimeSpan.FromSeconds(5) }, null, TestContext.Current.CancellationToken);

            Assert.Equal(ErrorCode.Sandbox, outcome.Error);
            Assert.Null(launcher.Last); // no Worker was created at all
            Assert.Equal(SandboxFailure.Setup, Assert.Single(log.Events).Sandbox);
        }
        finally
        {
            setup.Remove();
        }
    }
}
