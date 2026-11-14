using System.Runtime.InteropServices;

namespace Kalei.Core.Platform;

/// <summary>
/// Starts a Worker in the operating-system sandbox (S1, S5). Ticket 07a defined the channel of one Worker; ticket 07b adds what
/// the sandbox needs (handles to inherit, limits) and the Windows implementation.
/// </summary>
public interface ISandboxLauncher
{
    /// <summary>Starts one Worker and returns at once, without waiting for its Hello. Throws if it cannot be started under the sandbox: a Worker never runs unsandboxed.</summary>
    IWorkerProcess Start(WorkerStart start);
}

/// <summary>What one Worker is started with, besides its two pipes (ADR 0003: handles, never paths).</summary>
public sealed record WorkerStart
{
    /// <summary>File handles the Worker inherits (the Source, read-only, and the Output, exclusive-create). It receives no other.</summary>
    public IReadOnlyList<SafeHandle> InheritedHandles { get; init; } = [];

    /// <summary>Memory cap of the Worker process (Limits table).</summary>
    public long MemoryLimitBytes { get; init; } = DefaultMemoryLimitBytes;

    /// <summary>CPU time cap of the Worker (a safety net behind the Core's wall-clock watchdog, which ends it first).</summary>
    public TimeSpan CpuTimeLimit { get; init; } = TimeSpan.FromMinutes(15);

    public const long DefaultMemoryLimitBytes = 2L * 1024 * 1024 * 1024;
}

/// <summary>Why the sandbox ended a Worker on its own, as opposed to the Worker ending itself or the Core killing it.</summary>
public enum WorkerEndCause
{
    None,
    MemoryLimit,
    TimeLimit,
}

/// <summary>One running Worker: the two ends of its pipes and the means to end it.</summary>
public interface IWorkerProcess : IDisposable
{
    /// <summary>The Core writes here; the Worker reads.</summary>
    Stream ToWorker { get; }

    /// <summary>The Worker writes here; the Core reads. Ends (reads 0 bytes) when the Worker has ended or closed its pipe.</summary>
    Stream FromWorker { get; }

    /// <summary>Set when the sandbox itself ended the Worker for exceeding a limit. Read after the Worker has ended.</summary>
    WorkerEndCause EndCause { get; }

    /// <summary>Completes when the Worker has ended.</summary>
    Task WaitForExitAsync();

    /// <summary>Ends the Worker and everything it started. Safe to call more than once and after it has ended.</summary>
    void Kill();
}
