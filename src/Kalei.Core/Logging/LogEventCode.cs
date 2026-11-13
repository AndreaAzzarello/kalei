namespace Kalei.Core.Logging;

/// <summary>
/// What happened, as a code and never as text (S10). Later tickets add codes and typed fields (Format, Module, error code)
/// to <see cref="LogEvent"/>; none of them may ever be a string, a path or a file name.
/// </summary>
public enum LogEventCode
{
    /// <summary>Network canary verdict (architecture.md, S5).</summary>
    NetCanary,

    /// <summary>A request to a Worker ended because the channel failed: protocol violation, crash or time-out (ipc-spec.md).</summary>
    IpcEnd,
}
