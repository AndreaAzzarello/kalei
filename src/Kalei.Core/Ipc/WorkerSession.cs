using System.Diagnostics;
using Kalei.Core.Logging;
using Kalei.Core.Platform;

namespace Kalei.Core.Ipc;

/// <summary>
/// Runs one request against one Worker (spec, "Session and sequence" and "Watchdog"): starts it, checks everything it sends,
/// ends it on the first violation or time-out, and always ends it before returning. A Worker serves exactly one request.
/// </summary>
public sealed class WorkerSession(ISandboxLauncher launcher, IModuleCatalog catalog, IKaleiLog log, WatchdogTimes times)
{
    public async Task<RequestOutcome> RunAsync(IpcMessage request, RequestOptions options, IProgress<ProgressMessage>? progress,
        CancellationToken cancellationToken)
    {
        if (request is not (ProbeMessage or PreviewMessage or ConvertMessage or CanaryMessage))
        {
            throw new ArgumentException("Only Probe, Preview, Convert and Canary are requests.", nameof(request));
        }

        var started = Stopwatch.GetTimestamp();
        IWorkerProcess process;
        try
        {
            process = launcher.Start();
        }
        catch (Exception)
        {
            // Whatever stopped the launcher, the answer is the same: no Worker runs unsandboxed (07b logs the detail).
            return new RequestOutcome { Error = ErrorCode.Sandbox };
        }

        using (process)
        {
            var conversation = new Conversation(process, request, options, catalog, times, progress);
            var outcome = await conversation.RunAsync(cancellationToken).ConfigureAwait(false);
            await conversation.TerminateAsync().ConfigureAwait(false);

            if (outcome.Reason is not null)
            {
                log.Write(new LogEvent(LogLevel.Error, LogEventCode.IpcEnd, Stopwatch.GetElapsedTime(started), outcome.Error, outcome.Reason, outcome.Phase));
            }

            return outcome;
        }
    }

    /// <summary>The state of one request. Single use.</summary>
    private sealed class Conversation(IWorkerProcess process, IpcMessage request, RequestOptions options, IModuleCatalog catalog,
        WatchdogTimes times, IProgress<ProgressMessage>? progress)
    {
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly TaskCompletionSource _cancelRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private IpcPhase _phase = IpcPhase.AwaitHello;
        private Task<IncomingFrame?>? _pending;
        private TimeSpan _lastFrame;
        private TimeSpan _requestStart;
        private TimeSpan _finalAt;
        private TimeSpan _cancelAt;
        private bool _cancelSeen;
        private bool _cancelSent;
        private double _lastFraction;
        private long _lastBytes;
        private ErrorCode? _workerError;
        private IpcMessage? _answer;
        private byte[]? _pixels;

        public async Task<RequestOutcome> RunAsync(CancellationToken cancellationToken)
        {
            using var registration = cancellationToken.Register(static s => ((TaskCompletionSource)s!).TrySetResult(), _cancelRequested);
            try
            {
                return await LoopAsync().ConfigureAwait(false);
            }
            catch (IpcException ex)
            {
                // After Cancel the outcome stays "cancelled" whatever the Worker does (spec, "Session and sequence").
                return _cancelSent
                    ? new RequestOutcome { Error = ErrorCode.Cancelled }
                    : new RequestOutcome { Error = ex.Code, Reason = ex.Reason, Phase = _phase };
            }
        }

        /// <summary>The Worker is always ended, success or not (lifecycle step 6), and a read still open is let go.</summary>
        public async Task TerminateAsync()
        {
            process.Kill();
            await Task.WhenAny(process.WaitForExitAsync(), Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
            if (_pending is { } pending)
            {
                await Task.WhenAny(pending, Task.Delay(TimeSpan.FromSeconds(1))).ConfigureAwait(false);
                _ = pending.ContinueWith(static t => _ = t.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            }
        }

        private async Task<RequestOutcome> LoopAsync()
        {
            var reader = new FrameReader(process.FromWorker, Direction.ToCore, new FrameRateLimiter(IpcLimits.MaxFramesPerSecond, TimeSpan.FromSeconds(1)));
            while (true)
            {
                _pending ??= reader.ReadAsync();
                var (limit, onExpiry) = NextDeadline();
                using var timer = new CancellationTokenSource();
                var expiry = Task.Delay(Max(limit - _clock.Elapsed, TimeSpan.Zero), timer.Token);
                Task[] waits = _cancelSeen ? [_pending, expiry] : [_pending, expiry, _cancelRequested.Task];
                var woken = await Task.WhenAny(waits).ConfigureAwait(false);
                await timer.CancelAsync().ConfigureAwait(false);

                if (woken == expiry)
                {
                    if (_cancelSent)
                    {
                        return new RequestOutcome { Error = ErrorCode.Cancelled };
                    }

                    return _phase == IpcPhase.Done ? Success() : throw new IpcException(onExpiry, ErrorCode.Time);
                }

                if (woken == _cancelRequested.Task)
                {
                    if (await OnCancelAsync().ConfigureAwait(false) is { } cancelled)
                    {
                        return cancelled;
                    }

                    continue;
                }

                var read = _pending;
                _pending = null;
                var frame = await read.ConfigureAwait(false); // throws IpcException on a violation
                if (frame is null)
                {
                    // The pipe ended between two frames.
                    return _cancelSent ? new RequestOutcome { Error = ErrorCode.Cancelled }
                        : _phase == IpcPhase.Done ? Success()
                        : throw new IpcException(ProtocolReason.PipeClosed, ErrorCode.Crash);
                }

                _lastFrame = _clock.Elapsed;
                if (_cancelSent)
                {
                    continue; // framing, caps and rate were checked by the reader; the content is ignored
                }

                switch (_phase)
                {
                    case IpcPhase.AwaitHello:
                        await OnHelloAsync(frame.Message).ConfigureAwait(false);
                        break;
                    case IpcPhase.Open:
                        if (OnOpen(frame))
                        {
                            _phase = IpcPhase.Done;
                            _finalAt = _clock.Elapsed;
                        }

                        break;
                    default:
                        throw new IpcException(ProtocolReason.OutOfSequence); // anything after the final frame
                }
            }
        }

        private (TimeSpan Limit, ProtocolReason Reason) NextDeadline()
        {
            if (_cancelSent)
            {
                return (_cancelAt + times.CancelGrace, ProtocolReason.Timeout);
            }

            return _phase switch
            {
                IpcPhase.AwaitHello => (times.Hello, ProtocolReason.HelloTimeout),
                IpcPhase.Done => (_finalAt + times.FinalGrace, ProtocolReason.Timeout),
                _ => _lastFrame + times.Heartbeat <= _requestStart + options.Timeout
                    ? (_lastFrame + times.Heartbeat, ProtocolReason.Heartbeat)
                    : (_requestStart + options.Timeout, ProtocolReason.Timeout),
            };
        }

        private async Task<RequestOutcome?> OnCancelAsync()
        {
            _cancelSeen = true;
            if (_phase == IpcPhase.AwaitHello)
            {
                return new RequestOutcome { Error = ErrorCode.Cancelled }; // nothing to ask: end it
            }

            if (_phase == IpcPhase.Done)
            {
                return null; // already finished
            }

            _cancelSent = true;
            _cancelAt = _clock.Elapsed;
            try
            {
                await WriteAsync(new CancelMessage()).ConfigureAwait(false);
            }
            catch (IpcException)
            {
                return new RequestOutcome { Error = ErrorCode.Cancelled };
            }

            return null;
        }

        private async Task OnHelloAsync(IpcMessage message)
        {
            if (message is not HelloMessage hello)
            {
                throw new IpcException(ProtocolReason.OutOfSequence);
            }

            if (hello.Protocol != IpcLimits.ProtocolVersion)
            {
                throw new IpcException(ProtocolReason.BadVersion);
            }

            if (hello.Modules!.Any(m => !catalog.HasModule(m.Id, m.Version)))
            {
                throw new IpcException(ProtocolReason.BadValue);
            }

            await WriteAsync(new HelloMessage { Protocol = IpcLimits.ProtocolVersion }).ConfigureAwait(false);
            await WriteAsync(request).ConfigureAwait(false);
            _phase = IpcPhase.Open;
            _requestStart = _lastFrame = _clock.Elapsed;
        }

        /// <summary>Returns true when the frame was the final one.</summary>
        private bool OnOpen(IncomingFrame frame)
        {
            switch (frame.Message)
            {
                case ProgressMessage p:
                    CheckProgress(p);
                    progress?.Report(p);
                    return false;
                case ErrorMessage e:
                    _workerError = e.Code;
                    return true;
                case ProbeResultMessage r when request is ProbeMessage:
                    Require(catalog.HasFormat(r.Format));
                    _answer = r;
                    return true;
                case PreviewDataMessage d when request is PreviewMessage preview:
                    Require(d.Width <= preview.MaxSize && d.Height <= preview.MaxSize);
                    _answer = d;
                    _pixels = frame.Pixels;
                    return true;
                case ResultMessage when request is ConvertMessage:
                case CanaryResultMessage when request is CanaryMessage:
                    _answer = frame.Message;
                    return true;
                default:
                    throw new IpcException(ProtocolReason.OutOfSequence); // wrong answer, Hello again, or any other type
            }
        }

        private void CheckProgress(ProgressMessage p)
        {
            if (p.Fraction is { } fraction)
            {
                Require(fraction >= _lastFraction);
                _lastFraction = fraction;
            }

            if (p.Bytes is { } bytes)
            {
                Require(bytes >= _lastBytes && (options.SourceSize is not { } size || bytes <= size));
                _lastBytes = bytes;
            }
        }

        private RequestOutcome Success() => new() { Error = _workerError, Answer = _answer, Pixels = _pixels };

        /// <summary>A write must not hang the Core: a Worker that does not read is ended like one that does not speak.</summary>
        private async Task WriteAsync(IpcMessage message)
        {
            var write = FrameWriter.WriteAsync(process.ToWorker, message);
            using var timer = new CancellationTokenSource();
            var guard = Task.Delay(times.Heartbeat, timer.Token);
            if (await Task.WhenAny(write, guard).ConfigureAwait(false) == guard)
            {
                _ = write.ContinueWith(static t => _ = t.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
                throw new IpcException(ProtocolReason.Heartbeat, ErrorCode.Time);
            }

            await timer.CancelAsync().ConfigureAwait(false);
            try
            {
                await write.ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException)
            {
                throw new IpcException(ProtocolReason.PipeClosed, ErrorCode.Crash);
            }
        }

        private static void Require(bool valid)
        {
            if (!valid)
            {
                throw new IpcException(ProtocolReason.BadValue);
            }
        }

        private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;
    }
}
