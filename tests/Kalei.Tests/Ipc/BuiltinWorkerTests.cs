using System.Diagnostics;
using System.Runtime.Versioning;
using Kalei.Core.Ipc;
using Kalei.Core.Logging;
using Kalei.Worker.Builtin;
using Xunit;

namespace Kalei.Tests.Ipc;

/// <summary>The real Worker executable (Kalei.Worker.Builtin) and the real Core session over real pipes, with no sandbox (that is ticket 07b).</summary>
[SupportedOSPlatform("windows")]
public sealed class BuiltinWorkerTests
{
    internal sealed class BuiltinCatalog : IModuleCatalog
    {
        public bool HasModule(string id, string version) => BuiltinModules.All.Any(m => m.Id == id && m.Version == version);

        public bool HasFormat(string id) => true;
    }

    internal sealed class NoLog : IKaleiLog
    {
        public List<LogEvent> Events { get; } = [];

        public void Write(LogEvent logEvent) => Events.Add(logEvent);
    }

    internal static string BuiltinExe => Path.Combine(AppContext.BaseDirectory, "Kalei.Worker.Builtin.exe");

    public static TheoryData<string> Requests() => ["probe", "preview", "convert", "canary"];

    [Theory]
    [MemberData(nameof(Requests))]
    public async Task EachRequest_ToTheRealWorker_IsAnsweredWithEFormat_OverRealPipes(string kind)
    {
        IpcMessage request = kind switch
        {
            "probe" => new ProbeMessage { Source = 4 },
            "preview" => new PreviewMessage { Source = 4, MaxSize = 64 },
            "convert" => FrameTests.ConvertSample(),
            _ => new CanaryMessage { LocalPort = 50000 },
        };
        var launcher = new ProcessLauncher(BuiltinExe);
        var log = new NoLog();
        var session = new WorkerSession(launcher, new BuiltinCatalog(), log, WatchdogTimes.Default);

        var outcome = await session.RunAsync(request, new RequestOptions { Timeout = TimeSpan.FromSeconds(30) }, null, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCode.Format, outcome.Error);
        Assert.Null(outcome.Reason); // the Worker spoke correctly; it just has no engine yet
        Assert.Empty(log.Events);
        Assert.True(launcher.LastWorkerEnded);
    }

    [Fact]
    public async Task WorkerStartedWithoutThePipes_RefusesToRun_WithCodeTwo()
    {
        using var process = Process.Start(new ProcessStartInfo(BuiltinExe) { UseShellExecute = false, CreateNoWindow = true })!;

        await process.WaitForExitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, process.ExitCode);
    }
}
