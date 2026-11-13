using System.IO.Pipelines;
using Kalei.Core.Ipc;
using Kalei.Worker.Builtin;
using Xunit;

namespace Kalei.Tests.Ipc;

/// <summary>The Worker side of the protocol (ticket 07a): what the Worker host sends, and how it treats a Core it does not trust.</summary>
public sealed class WorkerHostTests
{
    /// <summary>Finishes only when the test says so, so the test never depends on how fast the machine is.</summary>
    private sealed class GatedResult : IRequestHandler
    {
        private readonly TaskCompletionSource _open = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Release() => _open.TrySetResult();

        public async Task<IpcMessage> HandleAsync(IpcMessage request, CancellationToken cancellationToken)
        {
            await _open.Task.WaitAsync(cancellationToken);
            return new ResultMessage { OutputBytes = 7 };
        }
    }

    private sealed class WaitsForever : IRequestHandler
    {
        public bool Cancelled { get; private set; }

        public async Task<IpcMessage> HandleAsync(IpcMessage request, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Cancelled = true;
                throw;
            }

            return new ResultMessage { OutputBytes = 0 };
        }
    }

    /// <summary>A Core played by the test, on in-memory pipes.</summary>
    private sealed class FakeCore
    {
        private readonly Pipe _toWorker = new();
        private readonly Pipe _fromWorker = new();
        private readonly Stream _write;
        private readonly FrameReader _read;

        public FakeCore(IRequestHandler handler, TimeSpan heartbeat)
        {
            _write = _toWorker.Writer.AsStream();
            var fromWorker = _fromWorker.Reader.AsStream();
            _read = new FrameReader(fromWorker, Direction.ToCore);
            Run = new WorkerHost(_toWorker.Reader.AsStream(), _fromWorker.Writer.AsStream(), handler, heartbeat)
                .RunAsync(TestContext.Current.CancellationToken)
                .ContinueWith(async t =>
                {
                    await _fromWorker.Writer.CompleteAsync(); // what Program does by ending the process
                    return t.Result;
                }, TaskScheduler.Default).Unwrap();
        }

        public Task<int> Run { get; }

        public async Task<IpcMessage?> ReadAsync() => (await _read.ReadAsync(TestContext.Current.CancellationToken))?.Message;

        public Task SendAsync(IpcMessage message) => FrameWriter.WriteAsync(_write, message, TestContext.Current.CancellationToken);

        public Task SendRawAsync(byte[] bytes) => _write.WriteAsync(bytes, TestContext.Current.CancellationToken).AsTask();

        public async Task HandshakeAsync()
        {
            Assert.IsType<HelloMessage>(await ReadAsync());
            await SendAsync(new HelloMessage { Protocol = IpcLimits.ProtocolVersion });
        }

        public Task CloseAsync() => _toWorker.Writer.CompleteAsync().AsTask();
    }

    private static readonly TimeSpan s_fast = TimeSpan.FromMilliseconds(50);

    [Fact]
    public async Task Hello_IsTheFirstFrame_SentBeforeTheCoreSaysAnything()
    {
        var core = new FakeCore(new UnavailableHandler(), s_fast);

        var hello = Assert.IsType<HelloMessage>(await core.ReadAsync());

        Assert.Equal(IpcLimits.ProtocolVersion, hello.Protocol);
        Assert.Equal(BuiltinModules.All.Select(m => (m.Id, m.Version)), hello.Modules!.Select(m => (m.Id, m.Version)));
        await core.CloseAsync();
        Assert.Equal(2, await core.Run); // the Core went away: nothing to do
    }

    [Theory]
    [InlineData("probe")]
    [InlineData("preview")]
    [InlineData("convert")]
    [InlineData("canary")]
    public async Task EachRequest_WithNoEngineYet_IsAnsweredWithEFormat_AndExitsZero(string kind)
    {
        var core = new FakeCore(new UnavailableHandler(), s_fast);
        await core.HandshakeAsync();
        await core.SendAsync(kind switch
        {
            "probe" => new ProbeMessage { Source = 4 },
            "preview" => new PreviewMessage { Source = 4, MaxSize = 64 },
            "convert" => FrameTests.ConvertSample(),
            _ => new CanaryMessage { LocalPort = 50000 },
        });

        var answer = await FinalAnswerAsync(core);

        Assert.Equal(ErrorCode.Format, Assert.IsType<ErrorMessage>(answer).Code);
        Assert.Equal(0, await core.Run);
    }

    /// <summary>Reads until the first frame that is not a Progress.</summary>
    private static async Task<IpcMessage> FinalAnswerAsync(FakeCore core)
    {
        while (await core.ReadAsync() is { } message)
        {
            if (message is not ProgressMessage)
            {
                return message;
            }
        }

        throw new InvalidOperationException("The Worker ended without an answer.");
    }

    [Fact]
    public async Task SlowRequest_SendsPureHeartbeatsAtASteadyPace_ThenTheAnswer()
    {
        var handler = new GatedResult();
        var core = new FakeCore(handler, TimeSpan.FromMilliseconds(100));
        await core.HandshakeAsync();
        await core.SendAsync(FrameTests.ConvertSample());

        var arrivals = new List<TimeSpan>();
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (arrivals.Count < 4)
        {
            var progress = Assert.IsType<ProgressMessage>(await core.ReadAsync());
            Assert.Null(progress.Fraction);
            Assert.Null(progress.Bytes);
            arrivals.Add(clock.Elapsed);
        }

        handler.Release();
        IpcMessage? last;
        while ((last = await core.ReadAsync()) is ProgressMessage)
        {
        }

        Assert.IsType<ResultMessage>(last);
        // Never faster than 10 per second: 100 ms apart, with a little slack for timer rounding.
        for (var i = 1; i < arrivals.Count; i++)
        {
            Assert.True(arrivals[i] - arrivals[i - 1] >= TimeSpan.FromMilliseconds(50), "heartbeats came faster than the interval");
        }

        Assert.Equal(0, await core.Run);
    }

    [Fact]
    public async Task Cancel_StopsTheRequest_AndTheWorkerExitsZeroWithoutAnAnswer()
    {
        var handler = new WaitsForever();
        var core = new FakeCore(handler, s_fast);
        await core.HandshakeAsync();
        await core.SendAsync(FrameTests.ConvertSample());
        await core.SendAsync(new CancelMessage());

        Assert.Equal(0, await core.Run);

        Assert.True(handler.Cancelled);
        while (await core.ReadAsync() is { } rest)
        {
            Assert.IsType<ProgressMessage>(rest); // heartbeats at most, never a Result or an Error
        }
    }

    public static TheoryData<string> BadCores() => new()
    {
        "HelloWithAnotherVersion",
        "HelloWithWorkerFields",
        "RequestBeforeHello",
        "AnswerInsteadOfRequest",
        "RequestWithABadValue",
        "OversizedFrame",
        "NotJson",
        "TwoRequests",
    };

    [Theory]
    [MemberData(nameof(BadCores))]
    public async Task ACoreThatBreaksTheProtocol_EndsTheWorkerWithCodeTwo_AndNothingIsAnswered(string misbehaviour)
    {
        var core = new FakeCore(new WaitsForever(), s_fast);
        Assert.IsType<HelloMessage>(await core.ReadAsync());

        switch (misbehaviour)
        {
            case "HelloWithAnotherVersion":
                await core.SendAsync(new HelloMessage { Protocol = 2 });
                break;
            case "HelloWithWorkerFields":
                await core.SendAsync(new HelloMessage { Protocol = 1, Worker = "x" });
                break;
            case "RequestBeforeHello":
                await core.SendAsync(new ProbeMessage { Source = 1 });
                break;
            case "AnswerInsteadOfRequest":
                await core.SendAsync(new HelloMessage { Protocol = 1 });
                await core.SendAsync(new ResultMessage { OutputBytes = 1 });
                break;
            case "RequestWithABadValue":
                await core.SendAsync(new HelloMessage { Protocol = 1 });
                await core.SendAsync(new ProbeMessage { Source = 0 });
                break;
            case "OversizedFrame":
                await core.SendAsync(new HelloMessage { Protocol = 1 });
                await core.SendRawAsync(FrameTests.PrefixOnly(2_000_000));
                break;
            case "NotJson":
                await core.SendAsync(new HelloMessage { Protocol = 1 });
                await core.SendRawAsync(FrameTests.Frame("this is not json at all"));
                break;
            case "TwoRequests":
                await core.SendAsync(new HelloMessage { Protocol = 1 });
                await core.SendAsync(new ProbeMessage { Source = 1 });
                await core.SendAsync(new ProbeMessage { Source = 1 }); // one request per Worker
                break;
        }

        Assert.Equal(2, await core.Run);
        while (await core.ReadAsync() is { } rest)
        {
            Assert.IsType<ProgressMessage>(rest);
        }
    }
}
