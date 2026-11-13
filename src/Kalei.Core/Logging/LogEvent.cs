namespace Kalei.Core.Logging;

/// <summary>One log entry. Typed fields only, so a path or file name cannot be given to the log (S10).</summary>
public readonly record struct LogEvent(LogLevel Level, LogEventCode Code, TimeSpan? Duration = null);
