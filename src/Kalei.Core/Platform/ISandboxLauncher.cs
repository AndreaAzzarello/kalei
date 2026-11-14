namespace Kalei.Core.Platform;

/// <summary>
/// Starts a Worker in the operating-system sandbox (S1, S5). Ticket 07a defines the part the IPC needs (the channel of one Worker);
/// ticket 07b adds what the sandbox needs (handles to inherit, memory cap) and the Windows implementation.
/// </summary>
public interface ISandboxLauncher
{
    /// <summary>Starts one Worker and returns at once, without waiting for its Hello. Throws if it cannot be started under the sandbox.</summary>
    IWorkerProcess Start();
}

/// <summary>One running Worker: the two ends of its pipes and the means to end it.</summary>
public interface IWorkerProcess : IDisposable
{
    /// <summary>The Core writes here; the Worker reads.</summary>
    Stream ToWorker { get; }

    /// <summary>The Worker writes here; the Core reads. Ends (reads 0 bytes) when the Worker has ended or closed its pipe.</summary>
    Stream FromWorker { get; }

    /// <summary>Completes when the Worker has ended.</summary>
    Task WaitForExitAsync();

    /// <summary>Ends the Worker and everything it started. Safe to call more than once and after it has ended.</summary>
    void Kill();
}
