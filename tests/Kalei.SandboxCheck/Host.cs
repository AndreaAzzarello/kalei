using Kalei.Core.Platform;
using Kalei.Platform.Windows.Sandbox;

namespace Kalei.SandboxCheck;

/// <summary>
/// Helper of the kill-on-close proof. Starts a Worker that reports its process id (probe "pid-wait"), prints that id and then either
/// waits forever ("wait", the caller kills this process) or ends normally ("exit"). The caller checks that the Worker went away with
/// its launcher (S1).
/// </summary>
internal static class Host
{
    public static async Task<int> RunAsync(string workerExe, string profile, string folder, string mode)
    {
        var launcher = new WindowsSandboxLauncher(new SandboxSetup(profile, folder), workerExe, "probe:pid-wait");
        var worker = launcher.Start(new WorkerStart()); // never disposed: the job handle closes with this process
        var line = await new StreamReader(worker.FromWorker).ReadLineAsync();
        Console.WriteLine(line);
        Console.Out.Flush();
        if (mode == "wait")
        {
            await Task.Delay(Timeout.Infinite);
        }

        return 0;
    }
}
