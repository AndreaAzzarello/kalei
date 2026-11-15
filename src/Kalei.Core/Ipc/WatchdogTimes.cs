namespace Kalei.Core.Ipc;

/// <summary>Timers of the watchdog (spec, "Watchdog"). Tests shorten them; the app uses <see cref="Default"/>.</summary>
public sealed record WatchdogTimes
{
    public static WatchdogTimes Default { get; } = new();

    /// <summary>
    /// From the launcher returning the process to the Worker's Hello. 15 s: after a reboot the reference VM showed 5.39 s at worst
    /// (a slow system, not a slow Worker; typical 0.7 s), and the Worker is already confined, so a longer wait costs no safety.
    /// </summary>
    public TimeSpan Hello { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>Without any frame while a request is open.</summary>
    public TimeSpan Heartbeat { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>From Cancel to the kill.</summary>
    public TimeSpan CancelGrace { get; init; } = TimeSpan.FromSeconds(3);

    /// <summary>From the final frame to the kill.</summary>
    public TimeSpan FinalGrace { get; init; } = TimeSpan.FromSeconds(3);
}
