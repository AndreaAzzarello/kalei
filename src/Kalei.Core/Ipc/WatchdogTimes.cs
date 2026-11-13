namespace Kalei.Core.Ipc;

/// <summary>Timers of the watchdog (spec, "Watchdog"). Tests shorten them; the app uses <see cref="Default"/>.</summary>
public sealed record WatchdogTimes
{
    public static WatchdogTimes Default { get; } = new();

    /// <summary>From the launcher returning the process to the Worker's Hello.</summary>
    public TimeSpan Hello { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Without any frame while a request is open.</summary>
    public TimeSpan Heartbeat { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>From Cancel to the kill.</summary>
    public TimeSpan CancelGrace { get; init; } = TimeSpan.FromSeconds(3);

    /// <summary>From the final frame to the kill.</summary>
    public TimeSpan FinalGrace { get; init; } = TimeSpan.FromSeconds(3);
}
