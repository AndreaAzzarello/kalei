using System.Runtime.Versioning;
using System.Text.Json;
using Kalei.Core.Platform;
using Kalei.Platform.Windows.Sandbox;
using Kalei.SandboxCheck;

[assembly: SupportedOSPlatform("windows")]

// Test-only tool of ticket 07b, never shipped. Drives the real Windows sandbox launcher; the kit for the reference VM runs it.
//   hello  <worker exe> <profile>          one start of the Worker: how long until its Hello (JSON line on stdout)
//   proofs <profile>                       every sandbox proof, each with its control outside the sandbox (JSON on stdout)
//   remove <profile> <folder>...           removes the profile and the folder access this tool made
//   host   <worker exe> <profile> <folder> <wait|exit>   helper of the kill-on-close proof (see Host.cs)
var json = new JsonSerializerOptions { WriteIndented = false };
switch (args)
{
    case ["hello", var workerExe, var profile]:
        Console.WriteLine(JsonSerializer.Serialize(await Hello.RunAsync(workerExe, profile), json));
        return 0;

    case ["proofs", var profile]:
        var results = await Proofs.RunAllAsync(profile);
        Console.WriteLine(JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        return results.Any(r => r.Verdict == "FAIL") ? 1 : 0;

    case ["remove", var profile, .. var folders]:
        var allGone = true;
        foreach (var folder in folders)
        {
            allGone &= new SandboxSetup(profile, folder).Remove();
        }

        Console.WriteLine(allGone ? "removed" : "NOT fully removed");
        return allGone ? 0 : 1;

    case ["host", var workerExe, var profile, var folder, var mode]:
        return await Host.RunAsync(workerExe, profile, folder, mode);

    default:
        await Console.Error.WriteLineAsync("usage: hello | proofs | remove | host (see Program.cs)");
        return 2;
}
