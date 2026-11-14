using Kalei.Core.Ipc;
using Kalei.Core.Platform;

namespace Kalei.Core.Logging;

/// <summary>
/// One log entry. Typed fields only (enums and numbers, never text), so a path or file name cannot be given to the log (S10).
/// Error, Reason and Phase belong to the IPC event (<see cref="LogEventCode.IpcEnd"/>); Sandbox and OsError (a number, never text)
/// to the start-up failure of a Worker (<see cref="LogEventCode.SandboxStart"/>).
/// </summary>
public readonly record struct LogEvent(
    LogLevel Level,
    LogEventCode Code,
    TimeSpan? Duration = null,
    ErrorCode? Error = null,
    ProtocolReason? Reason = null,
    IpcPhase? Phase = null,
    SandboxFailure? Sandbox = null,
    int? OsError = null);
