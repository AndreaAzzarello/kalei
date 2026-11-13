using Kalei.Core.Ipc;

namespace Kalei.Core.Logging;

/// <summary>
/// One log entry. Typed fields only (enums and numbers, never text), so a path or file name cannot be given to the log (S10).
/// Error, Reason and Phase belong to the IPC event (<see cref="LogEventCode.IpcEnd"/>).
/// </summary>
public readonly record struct LogEvent(
    LogLevel Level,
    LogEventCode Code,
    TimeSpan? Duration = null,
    ErrorCode? Error = null,
    ProtocolReason? Reason = null,
    IpcPhase? Phase = null);
