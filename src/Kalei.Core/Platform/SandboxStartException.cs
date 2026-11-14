namespace Kalei.Core.Platform;

/// <summary>Which step of starting a Worker under the sandbox failed (a closed list: it goes to the log, S10).</summary>
public enum SandboxFailure
{
    Other,

    /// <summary>First-run set-up is missing and could not be made: the profile or the folder access.</summary>
    Setup,
    Pipes,
    Handles,
    Process,
    Job,
}

/// <summary>The sandbox could not start a Worker. The message holds no path and no file name.</summary>
public sealed class SandboxStartException(SandboxFailure failure, int osError = 0) : Exception($"The sandbox could not start a Worker ({failure}, OS error {osError}).")
{
    public SandboxFailure Failure { get; } = failure;

    /// <summary>The Win32 error or HRESULT, 0 when there is none.</summary>
    public int OsError { get; } = osError;
}
