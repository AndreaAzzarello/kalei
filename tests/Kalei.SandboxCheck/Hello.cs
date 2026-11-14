using System.Diagnostics;
using Kalei.Core.Ipc;
using Kalei.Core.Platform;
using Kalei.Platform.Windows.Sandbox;

namespace Kalei.SandboxCheck;

/// <summary>One start of the real Worker in the sandbox, timed up to its Hello. The watchdog's timer starts when the launcher returns, so that is the figure that matters (<see cref="HelloResult.HelloAfterStartMs"/>).</summary>
internal static class Hello
{
    public static async Task<HelloResult> RunAsync(string workerExe, string profile)
    {
        var setup = new SandboxSetup(profile, Path.GetDirectoryName(Path.GetFullPath(workerExe))!);

        var t0 = Stopwatch.GetTimestamp();
        var status = setup.Prepare(); // profile and folder access: once, on the first run
        var prepared = Stopwatch.GetTimestamp();
        if (status != SandboxSetupStatus.Ready)
        {
            return new HelloResult(false, status.ToString(), "setup", Ms(t0, prepared), 0, 0, 0);
        }

        try
        {
            var launcher = new WindowsSandboxLauncher(setup, workerExe);
            var beforeStart = Stopwatch.GetTimestamp();
            using var worker = launcher.Start(new WorkerStart());
            var started = Stopwatch.GetTimestamp();

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var frame = await new FrameReader(worker.FromWorker, Direction.ToCore).ReadAsync(timeout.Token);
            var hello = Stopwatch.GetTimestamp();
            worker.Kill();

            return new HelloResult(frame?.Message is HelloMessage, status.ToString(), frame is null ? "no frame" : frame.Message.GetType().Name,
                Ms(t0, prepared), Ms(beforeStart, started), Ms(started, hello), Ms(beforeStart, hello));
        }
        catch (SandboxStartException e)
        {
            return new HelloResult(false, status.ToString(), $"start failed: {e.Failure} {e.OsError}", Ms(t0, prepared), 0, 0, 0);
        }
        catch (OperationCanceledException)
        {
            return new HelloResult(false, status.ToString(), "no Hello in 30 s", Ms(t0, prepared), 0, 0, 0);
        }
    }

    private static double Ms(long from, long to) => Math.Round(Stopwatch.GetElapsedTime(from, to).TotalMilliseconds, 1);
}

/// <param name="Ok">The first frame was the Hello.</param>
/// <param name="Setup">Result of the first-run set-up.</param>
/// <param name="Detail">What the first frame was, or why there was none.</param>
/// <param name="PrepareMs">Set-up (profile, folder access): the one-off cost, outside the watchdog.</param>
/// <param name="StartMs">The launcher's Start call.</param>
/// <param name="HelloAfterStartMs">From the launcher returning to the Hello read: what the 5 s handshake timer measures.</param>
/// <param name="HelloTotalMs">From just before Start to the Hello read (the figure of ticket 07a, process creation included).</param>
internal sealed record HelloResult(bool Ok, string Setup, string Detail, double PrepareMs, double StartMs, double HelloAfterStartMs, double HelloTotalMs);
