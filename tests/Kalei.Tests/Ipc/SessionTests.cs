using Kalei.Core.Ipc;
using Kalei.Core.Logging;
using Kalei.Core.Platform;
using Xunit;

namespace Kalei.Tests.Ipc;

/// <summary>
/// The Core's session against scripted Workers: the success paths, and cases 9 to 15 of the table in docs/ipc-spec.md (cases 1 to 8
/// and 12 are also run here end to end, through real frames). Every test runs on both transports.
/// </summary>
public sealed class SessionTests
{
    private sealed class FakeCatalog : IModuleCatalog
    {
        public bool HasModule(string id, string version) => id == "images" && version == "1.0.0";

        public bool HasFormat(string id) => id is "png" or "webp";
    }

    private sealed class ListLog : IKaleiLog
    {
        public List<LogEvent> Events { get; } = [];

        public void Write(LogEvent logEvent) => Events.Add(logEvent);
    }

    private static readonly ProbeMessage s_probe = new() { Source = 1 };
    private static readonly PreviewMessage s_preview = new() { Source = 1, MaxSize = 256 };
    private static readonly PreviewMessage s_smallPreview = new() { Source = 1, MaxSize = 16 };
    private static readonly CanaryMessage s_canary = new() { LocalPort = 50000 };

    public static TheoryData<Transport> Both() => new() { Transport.InProcess, Transport.Process };

    private static async Task<(RequestOutcome Outcome, ListLog Log, ITestLauncher Launcher)> RunAsync(
        Transport transport, string behaviour, IpcMessage request, long? sourceSize = null, TimeSpan? timeout = null,
        IProgress<ProgressMessage>? progress = null, CancellationTokenSource? cancelSource = null)
    {
        var launcher = TestLaunchers.For(transport, behaviour);
        var log = new ListLog();
        var session = new WorkerSession(launcher, new FakeCatalog(), log, TestLaunchers.Times(transport));
        var options = new RequestOptions { Timeout = timeout ?? TimeSpan.FromSeconds(30), SourceSize = sourceSize };
        var outcome = await session.RunAsync(request, options, progress, cancelSource?.Token ?? TestContext.Current.CancellationToken);
        return (outcome, log, launcher);
    }

    // --- success -----------------------------------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(Both))]
    public async Task Probe_Normal_ReturnsTheProbeResult(Transport transport)
    {
        var (outcome, log, launcher) = await RunAsync(transport, "Normal", s_probe);

        Assert.True(outcome.Succeeded);
        var answer = Assert.IsType<ProbeResultMessage>(outcome.Answer);
        Assert.Equal("png", answer.Format);
        Assert.Empty(log.Events);
        Assert.True(launcher.LastWorkerEnded);
    }

    [Theory]
    [MemberData(nameof(Both))]
    public async Task Preview_Normal_ReturnsThePixels(Transport transport)
    {
        var (outcome, _, launcher) = await RunAsync(transport, "Normal", s_preview);

        Assert.True(outcome.Succeeded);
        Assert.Equal(16, outcome.Pixels!.Length);
        Assert.True(launcher.LastWorkerEnded);
    }

    [Theory]
    [MemberData(nameof(Both))]
    public async Task Convert_Normal_ReturnsTheResultAndReportsProgress(Transport transport)
    {
        var seen = new List<ProgressMessage>();
        var (outcome, _, launcher) = await RunAsync(transport, "Normal", FrameTests.ConvertSample(), progress: new Progress<ProgressMessage>(seen.Add));

        Assert.True(outcome.Succeeded);
        var result = Assert.IsType<ResultMessage>(outcome.Answer);
        Assert.Equal(10, result.OutputBytes);
        Assert.Equal([Notice.FirstFrameOnly], result.Warnings);
        await Task.Delay(50, TestContext.Current.CancellationToken); // Progress<T> posts to the thread pool
        Assert.Contains(seen, p => p.Fraction == 0.5);
        Assert.True(launcher.LastWorkerEnded);
    }

    [Theory]
    [MemberData(nameof(Both))]
    public async Task Canary_Normal_ReturnsTheCanaryResult(Transport transport)
    {
        var (outcome, _, _) = await RunAsync(transport, "Normal", s_canary);

        Assert.True(outcome.Succeeded);
        Assert.IsType<CanaryResultMessage>(outcome.Answer);
    }

    [Theory]
    [MemberData(nameof(Both))]
    public async Task WorkerError_IsThatCode_WithNoReasonAndNoLogEvent(Transport transport)
    {
        var (outcome, log, _) = await RunAsync(transport, "WorkerError", s_probe);

        Assert.Equal(ErrorCode.Corrupt, outcome.Error);
        Assert.Null(outcome.Reason);
        Assert.Empty(log.Events);
    }

    [Fact]
    public async Task NonZeroExitCodeAfterAValidResult_IsIgnored_CaseFifteen()
    {
        var (outcome, _, launcher) = await RunAsync(Transport.Process, "NonZeroExitAfterResult", FrameTests.ConvertSample());

        Assert.True(outcome.Succeeded);
        Assert.True(launcher.LastWorkerEnded);
    }

    // --- the table: every way a Worker can go wrong ------------------------------------------------------------------

    public static TheoryData<string, string, ErrorCode, ProtocolReason> Failures()
    {
        var data = new TheoryData<string, string, ErrorCode, ProtocolReason>();
        void Add(string behaviour, string request, ErrorCode code, ProtocolReason reason) => data.Add(behaviour, request, code, reason);

        // Case 1 to 8: the frame itself.
        Add("HugeLength", "probe", ErrorCode.Protocol, ProtocolReason.FrameTooLarge);
        Add("NegativeLength", "probe", ErrorCode.Protocol, ProtocolReason.FrameTooLarge);
        Add("ZeroLength", "probe", ErrorCode.Protocol, ProtocolReason.BadLength);
        Add("TooLargeForType", "probe", ErrorCode.Protocol, ProtocolReason.FrameTooLargeForType);
        Add("BadJsonPrefix", "probe", ErrorCode.Protocol, ProtocolReason.BadJson);
        Add("InvalidJson", "probe", ErrorCode.Protocol, ProtocolReason.BadJson);
        Add("DuplicateKey", "probe", ErrorCode.Protocol, ProtocolReason.BadJson);
        Add("NullValue", "probe", ErrorCode.Protocol, ProtocolReason.BadJson);
        Add("TrailingBytes", "probe", ErrorCode.Protocol, ProtocolReason.BadJson);
        Add("UnknownType", "probe", ErrorCode.Protocol, ProtocolReason.UnknownType);
        Add("UnknownField", "probe", ErrorCode.Protocol, ProtocolReason.UnknownField);
        Add("MissingField", "convert", ErrorCode.Protocol, ProtocolReason.MissingField);
        Add("BadValue", "probe", ErrorCode.Protocol, ProtocolReason.BadValue);
        Add("ForbiddenErrorCode", "probe", ErrorCode.Protocol, ProtocolReason.BadValue);
        Add("ProgressBothFields", "probe", ErrorCode.Protocol, ProtocolReason.BadValue);
        Add("ProgressDecreasing", "convert", ErrorCode.Protocol, ProtocolReason.BadValue);
        Add("ProgressBytesOverSource", "convert", ErrorCode.Protocol, ProtocolReason.BadValue);
        Add("PreviewLengthMismatch", "preview", ErrorCode.Protocol, ProtocolReason.BadValue);
        Add("PreviewTooBig", "smallpreview", ErrorCode.Protocol, ProtocolReason.BadValue);
        Add("HelloBadModule", "probe", ErrorCode.Protocol, ProtocolReason.BadValue);

        // Case 9: sequence.
        Add("FirstFrameIsProgress", "probe", ErrorCode.Protocol, ProtocolReason.OutOfSequence);
        Add("AlwaysProbeResult", "convert", ErrorCode.Protocol, ProtocolReason.OutOfSequence);
        Add("TwoFinals", "convert", ErrorCode.Protocol, ProtocolReason.OutOfSequence);
        Add("FrameAfterFinal", "convert", ErrorCode.Protocol, ProtocolReason.OutOfSequence);
        Add("HelloInOpen", "probe", ErrorCode.Protocol, ProtocolReason.OutOfSequence);

        // Case 10, 11: version, rate.
        Add("BadVersion", "probe", ErrorCode.Protocol, ProtocolReason.BadVersion);
        Add("Flood", "probe", ErrorCode.Protocol, ProtocolReason.FrameRate);

        // Case 12: the pipe closes.
        Add("CloseMidFrame", "probe", ErrorCode.Crash, ProtocolReason.PipeClosed);
        Add("ExitBeforeFinal", "probe", ErrorCode.Crash, ProtocolReason.PipeClosed);

        // Case 13: time.
        Add("Silent", "probe", ErrorCode.Time, ProtocolReason.HelloTimeout);
        Add("StallAfterHello", "probe", ErrorCode.Time, ProtocolReason.Heartbeat);
        Add("Heartbeats", "timeout", ErrorCode.Time, ProtocolReason.Timeout);
        return data;
    }

    private static IpcMessage RequestFor(string kind) => kind switch
    {
        "probe" => s_probe,
        "preview" => s_preview,
        "smallpreview" => s_smallPreview,
        "convert" or "timeout" => FrameTests.ConvertSample(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    [Theory]
    [MemberData(nameof(Failures))]
    public async Task InProcess_Violation_EndsTheWorkerWithTheCodeAndTheReason(string behaviour, string request, ErrorCode code, ProtocolReason reason)
    {
        var (outcome, log, launcher) = await RunAsync(Transport.InProcess, behaviour, RequestFor(request),
            sourceSize: 1000, timeout: request == "timeout" ? TimeSpan.FromMilliseconds(400) : null);

        Assert.Equal(code, outcome.Error);
        Assert.Equal(reason, outcome.Reason);
        Assert.Null(outcome.Answer);
        var logged = Assert.Single(log.Events);
        Assert.Equal((LogEventCode.IpcEnd, code, reason), (logged.Code, logged.Error, logged.Reason));
        Assert.True(launcher.LastWorkerEnded, "the Worker must always be ended");
    }

    [Theory]
    [MemberData(nameof(Failures))]
    public async Task Process_Violation_EndsTheWorkerWithTheCodeAndTheReason(string behaviour, string request, ErrorCode code, ProtocolReason reason)
    {
        var (outcome, log, launcher) = await RunAsync(Transport.Process, behaviour, RequestFor(request),
            sourceSize: 1000, timeout: request == "timeout" ? TimeSpan.FromMilliseconds(1500) : null);

        Assert.Equal(code, outcome.Error);
        Assert.Equal(reason, outcome.Reason);
        Assert.Single(log.Events);
        Assert.True(launcher.LastWorkerEnded, "the Worker must always be ended");
    }

    // --- case 14: Cancel ---------------------------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(Both))]
    public async Task Cancel_ToAWorkerThatObeys_EndsAsCancelled(Transport transport)
    {
        using var cancel = new CancellationTokenSource();
        cancel.CancelAfter(TimeSpan.FromMilliseconds(400));

        var (outcome, log, launcher) = await RunAsync(transport, "CancelRespect", FrameTests.ConvertSample(), cancelSource: cancel);

        Assert.Equal(ErrorCode.Cancelled, outcome.Error);
        Assert.Null(outcome.Reason);
        Assert.Empty(log.Events);
        Assert.True(launcher.LastWorkerEnded);
    }

    [Theory]
    [MemberData(nameof(Both))]
    public async Task Cancel_ToAWorkerThatIgnoresIt_IsKilledAfterTheGrace_AndStaysCancelled(Transport transport)
    {
        using var cancel = new CancellationTokenSource();
        cancel.CancelAfter(TimeSpan.FromMilliseconds(400));
        var started = System.Diagnostics.Stopwatch.StartNew();

        var (outcome, _, launcher) = await RunAsync(transport, "Heartbeats", FrameTests.ConvertSample(), cancelSource: cancel);

        Assert.Equal(ErrorCode.Cancelled, outcome.Error);
        Assert.True(started.Elapsed < TimeSpan.FromSeconds(10));
        Assert.True(launcher.LastWorkerEnded);
    }

    [Theory]
    [MemberData(nameof(Both))]
    public async Task Cancel_BeforeTheHello_KillsTheWorker(Transport transport)
    {
        using var cancel = new CancellationTokenSource();
        cancel.CancelAfter(TimeSpan.FromMilliseconds(100));

        var (outcome, _, launcher) = await RunAsync(transport, "Silent", s_probe, cancelSource: cancel);

        Assert.Equal(ErrorCode.Cancelled, outcome.Error);
        Assert.True(launcher.LastWorkerEnded);
    }

    // --- E_SANDBOX ---------------------------------------------------------------------------------------------------

    private sealed class RefusingLauncher : ISandboxLauncher
    {
        public IWorkerProcess Start() => throw new InvalidOperationException("no sandbox");
    }

    [Fact]
    public async Task LauncherThatCannotStart_IsESandbox_AndNothingRuns()
    {
        var session = new WorkerSession(new RefusingLauncher(), new FakeCatalog(), new ListLog(), WatchdogTimes.Default);

        var outcome = await session.RunAsync(s_probe, new RequestOptions { Timeout = TimeSpan.FromSeconds(1) }, null, TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCode.Sandbox, outcome.Error);
        Assert.Null(outcome.Answer);
    }

    [Fact]
    public async Task ARequestThatIsNotARequest_IsRefused()
    {
        var session = new WorkerSession(TestLaunchers.For(Transport.InProcess, "Normal"), new FakeCatalog(), new ListLog(), WatchdogTimes.Default);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            session.RunAsync(new CancelMessage(), new RequestOptions { Timeout = TimeSpan.FromSeconds(1) }, null, TestContext.Current.CancellationToken));
    }
}
