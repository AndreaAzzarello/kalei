using System.Runtime.Versioning;
using Kalei.Core.Platform;
using Kalei.Platform.Windows.Sandbox;

[assembly: SupportedOSPlatform("windows")]

// Usage: Kalei.SandboxCheck host <worker exe> <profile name> <worker folder> <wait|exit>
// Starts a Worker that reports its process id (probe "pid-wait"), prints that id and then either waits forever ("wait", the test
// kills this process) or ends normally ("exit"). The test then checks that the Worker went away with its launcher (kill-on-close, S1).
if (args is ["host", var workerExe, var profile, var folder, var mode])
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

return 2;
