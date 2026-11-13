using System.Diagnostics;
using System.IO.Pipelines;
using System.IO.Pipes;
using Kalei.Core.Ipc;
using Kalei.Core.Platform;
using Kalei.FakeWorker;

namespace Kalei.Tests.Ipc;

public enum Transport
{
    /// <summary>The scripted Worker runs in this process on in-memory pipes.</summary>
    InProcess,

    /// <summary>The scripted Worker is the Kalei.FakeWorker process on anonymous pipes (no sandbox: that is ticket 07b).</summary>
    Process,
}

internal interface IEnded
{
    bool Ended { get; }
}

/// <summary>A launcher that remembers whether the last Worker it started has ended.</summary>
internal interface ITestLauncher : ISandboxLauncher
{
    bool LastWorkerEnded { get; }
}

internal static class TestLaunchers
{
    public static ITestLauncher For(Transport transport, string behaviour) => transport switch
    {
        Transport.InProcess => new InProcessLauncher(behaviour),
        _ => new ProcessLauncher(Path.Combine(AppContext.BaseDirectory, "Kalei.FakeWorker.exe"), behaviour),
    };

    public static WatchdogTimes Times(Transport transport) => transport == Transport.InProcess
        ? new WatchdogTimes
        {
            Hello = TimeSpan.FromMilliseconds(1000),
            Heartbeat = TimeSpan.FromMilliseconds(500),
            CancelGrace = TimeSpan.FromMilliseconds(400),
            FinalGrace = TimeSpan.FromMilliseconds(400),
        }
        : new WatchdogTimes
        {
            Hello = TimeSpan.FromSeconds(5),
            Heartbeat = TimeSpan.FromMilliseconds(2000),
            CancelGrace = TimeSpan.FromMilliseconds(500),
            FinalGrace = TimeSpan.FromMilliseconds(500),
        };
}

internal sealed class InProcessLauncher(string behaviour) : ITestLauncher
{
    private IEnded? _last;

    public bool LastWorkerEnded => _last is { Ended: true };

    public IWorkerProcess Start()
    {
        var worker = new InProcessWorker(behaviour);
        _last = worker;
        return worker;
    }
}

internal sealed class InProcessWorker : IWorkerProcess, IEnded
{
    private readonly Pipe _toWorker = new();
    private readonly Pipe _fromWorker = new();
    private readonly CancellationTokenSource _killed = new();
    private readonly Task _run;

    public InProcessWorker(string behaviour)
    {
        ToWorker = _toWorker.Writer.AsStream();
        FromWorker = _fromWorker.Reader.AsStream();
        _run = Task.Run(async () =>
        {
            try
            {
                await Behaviours.RunAsync(behaviour, _toWorker.Reader.AsStream(), _fromWorker.Writer.AsStream(), _killed.Token);
            }
            catch (OperationCanceledException)
            {
                // killed
            }
            finally
            {
                await _fromWorker.Writer.CompleteAsync();
                await _toWorker.Reader.CompleteAsync();
            }
        });
    }

    public bool Ended => _run.IsCompleted;

    public Stream ToWorker { get; }

    public Stream FromWorker { get; }

    public Task WaitForExitAsync() => _run;

    public void Kill() => _killed.Cancel();

    public void Dispose() => Kill();
}

/// <summary>Starts a real process with two anonymous pipes whose client ends are inherited (no sandbox).</summary>
internal sealed class ProcessLauncher(string fileName, params string[] leadingArguments) : ITestLauncher
{
    // A handle that is inheritable is inherited by any process started at the same moment: one launch at a time.
    private static readonly Lock s_gate = new();

    private IEnded? _last;

    public bool LastWorkerEnded => _last is { Ended: true };

    public IWorkerProcess Start()
    {
        lock (s_gate)
        {
            var toWorker = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            var fromWorker = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            var info = new ProcessStartInfo(fileName) { UseShellExecute = false, CreateNoWindow = true };
            foreach (var argument in leadingArguments)
            {
                info.ArgumentList.Add(argument);
            }

            info.ArgumentList.Add(toWorker.GetClientHandleAsString());
            info.ArgumentList.Add(fromWorker.GetClientHandleAsString());
            var process = Process.Start(info) ?? throw new InvalidOperationException("The process did not start.");
            toWorker.DisposeLocalCopyOfClientHandle();
            fromWorker.DisposeLocalCopyOfClientHandle();
            var worker = new ProcessWorker(process, toWorker, fromWorker);
            _last = worker;
            return worker;
        }
    }
}

internal sealed class ProcessWorker(Process process, Stream toWorker, Stream fromWorker) : IWorkerProcess, IEnded
{
    private bool? _endedWhenDisposed;

    public bool Ended => _endedWhenDisposed ?? process.HasExited;

    public Stream ToWorker { get; } = toWorker;

    public Stream FromWorker { get; } = fromWorker;

    public Task WaitForExitAsync() => process.WaitForExitAsync();

    public void Kill()
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // already ended
        }
    }

    public void Dispose()
    {
        Kill();
        _endedWhenDisposed = process.WaitForExit(2000);
        ToWorker.Dispose();
        FromWorker.Dispose();
        process.Dispose();
    }
}
