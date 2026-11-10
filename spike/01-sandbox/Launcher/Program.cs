using SandboxSpike.Launcher;

// Commands:
//   proofs [--worker <published worker dir>] [--out <report.json>]   run all ticket-01 proofs
//   host-hang --worker <installed worker dir>                         stand-in for the app: starts a hung Worker, prints its PID,
//                                                                     waits for "exit" on stdin (or to be killed) and must take the Worker with it
static string? Opt(string[] a, string name) { var i = Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }

if (args.Length == 0) { Console.Error.WriteLine("usage: proofs | host-hang"); return 2; }

switch (args[0])
{
    case "proofs":
    {
        var worker = Opt(args, "--worker") ?? Path.Combine(AppContext.BaseDirectory, "worker");
        var suite = new ProofSuite(worker);
        suite.RunAll();
        suite.WriteReport(Opt(args, "--out") ?? "report.json");
        var failed = suite.Results.Count(r => r.Status == "FAIL");
        var inconclusive = suite.Results.Count(r => r.Status == "INCONCLUSIVE");
        Console.WriteLine($"\nPASS {suite.Results.Count(r => r.Passed)}  FAIL {failed}  INCONCLUSIVE {inconclusive}");
        return failed == 0 ? 0 : 1;
    }
    case "host-hang":
    {
        var dir = Opt(args, "--worker")!;
        using var p = SandboxProcess.Start(new RunOptions { Mode = "hang", WorkerDir = dir }, out var err);
        Console.WriteLine(err == 0 ? $"PID {p.ProcessId}" : $"ERR {err}");
        Console.Out.Flush();
        Console.In.ReadLine(); // the app is "running"; closing it (or killing it) must end the Worker
        return 0;
    }
    default:
        Console.Error.WriteLine("unknown command");
        return 2;
}
