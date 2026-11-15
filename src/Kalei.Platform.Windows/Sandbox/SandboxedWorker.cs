using System.IO.Pipes;
using Kalei.Core.Platform;
using Microsoft.Win32.SafeHandles;

namespace Kalei.Platform.Windows.Sandbox;

/// <summary>A Worker running inside its Job Object. Letting go of the job kills it (kill-on-close), so it cannot outlive the Core.</summary>
internal sealed class SandboxedWorker : IWorkerProcess
{
    private readonly AnonymousPipeServerStream _toWorker;
    private readonly AnonymousPipeServerStream _fromWorker;
    private readonly SafeProcessHandle _process;
    private readonly SafeKernelHandle _job;
    private readonly SafeKernelHandle _port;
    private readonly ManualResetEvent _exited;
    private readonly RegisteredWaitHandle _registration;
    private readonly TaskCompletionSource _exit = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Lock _gate = new();
    private readonly long _memoryLimit;
    private WorkerEndCause _cause;
    private bool _disposed;

    public SandboxedWorker(AnonymousPipeServerStream toWorker, AnonymousPipeServerStream fromWorker, SafeProcessHandle process, SafeKernelHandle job, SafeKernelHandle port, long memoryLimit)
    {
        _memoryLimit = memoryLimit;
        _toWorker = toWorker;
        _fromWorker = fromWorker;
        _process = process;
        _job = job;
        _port = port;

        // A wait on the process handle (not owned by this event) completes the task when the Worker ends.
        _exited = new ManualResetEvent(false) { SafeWaitHandle = new SafeWaitHandle(process.DangerousGetHandle(), ownsHandle: false) };
        _registration = ThreadPool.RegisterWaitForSingleObject(_exited, static (state, _) => ((TaskCompletionSource)state!).TrySetResult(), _exit, Timeout.Infinite, executeOnlyOnce: true);
    }

    public Stream ToWorker => _toWorker;

    public Stream FromWorker => _fromWorker;

    public WorkerEndCause EndCause
    {
        get
        {
            lock (_gate)
            {
                // The job posts a message when it ends the process for a limit, before the process is gone.
                while (!_disposed && _cause == WorkerEndCause.None
                    && Native.GetQueuedCompletionStatus(_port, out var message, out _, out _, 0))
                {
                    _cause = message switch
                    {
                        Native.JobMsgProcessMemoryLimit or Native.JobMsgJobMemoryLimit => WorkerEndCause.MemoryLimit,
                        Native.JobMsgEndOfJobTime => WorkerEndCause.TimeLimit,
                        _ => WorkerEndCause.None,
                    };
                }

                if (!_disposed && _cause == WorkerEndCause.None && _exit.Task.IsCompleted && PeakMemoryReachedTheGcLimit())
                {
                    _cause = WorkerEndCause.MemoryLimit;
                }

                return _cause;
            }
        }
    }

    /// <summary>
    /// The .NET GC reads the job's memory cap and throws OutOfMemoryException itself at 75% of it, so the kernel never sees a
    /// violation and posts no message (measured, ticket 07b; the spike's Worker stopped at 72% of 2 GB). A Worker that ended with a
    /// peak of at least 70% of the cap is a Worker the cap ended; a native allocation that fails at the cap posts a message instead.
    /// </summary>
    private bool PeakMemoryReachedTheGcLimit() =>
        Native.QueryInformationJobObject(_job, Native.JobObjectExtendedLimitInformation, out var limits, System.Runtime.InteropServices.Marshal.SizeOf<JobExtendedLimit>(), 0)
        && (double)limits.PeakProcessMemoryUsed >= _memoryLimit * 0.7;

    public Task WaitForExitAsync() => _exit.Task;

    public void Kill()
    {
        lock (_gate)
        {
            if (!_disposed)
            {
                Native.TerminateJobObject(_job, 1); // the Worker and anything it started
            }
        }
    }

    public void Dispose()
    {
        // Read the cause first: it is lost when the port closes.
        _ = EndCause;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            Native.TerminateJobObject(_job, 1);
            _disposed = true;
        }

        _registration.Unregister(null);
        _exited.Dispose();
        _toWorker.Dispose();
        _fromWorker.Dispose();
        _job.Dispose();
        _port.Dispose();
        _process.Dispose();
    }
}
