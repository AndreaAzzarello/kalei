using System.Diagnostics;
using SandboxSpike.Launcher;
using Xunit;

namespace SandboxSpike.Tests;

/// <summary>Runs the whole suite once (it takes a few minutes) and exposes each proof as a test.</summary>
public sealed class SuiteFixture : IDisposable
{
    public ProofSuite Suite { get; }

    public SuiteFixture()
    {
        var spike = FindSpikeRoot();
        var worker = Path.Combine(spike, "artifacts", "worker");
        if (!File.Exists(Path.Combine(worker, "SandboxSpike.Worker.exe")))
        {
            var psi = new ProcessStartInfo("dotnet", $"publish \"{Path.Combine(spike, "Worker")}\" -c Release -o \"{worker}\" --nologo -v q")
            { UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi)!;
            p.WaitForExit();
            if (p.ExitCode != 0) throw new InvalidOperationException("Worker publish failed");
        }
        Suite = new ProofSuite(worker);
        Suite.RunAll();
        Suite.WriteReport(Path.Combine(spike, "results", "report.json"));
    }

    private static string FindSpikeRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "README.md")) ) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("spike root not found");
    }

    public void Dispose() { }
}

public class SandboxProofTests(SuiteFixture f) : IClassFixture<SuiteFixture>
{
    private void MustPass(string id)
    {
        var r = f.Suite.Results.SingleOrDefault(x => x.Id == id);
        Assert.NotNull(r);
        // INCONCLUSIVE means a positive control failed (for example no internet): it is not a pass.
        Assert.True(r!.Passed, $"{id}: {r.Status} — {r.Detail}");
    }

    [Fact] public void W4_worker_loads_from_per_user_folder_after_acl() => MustPass("W4");
    [Fact] public void W1_png_to_avif_in_appcontainer() => MustPass("W1-avif");
    [Fact] public void W1_png_to_webp_in_appcontainer() => MustPass("W1-webp");
    [Fact] public void W2_tcp_fails() => MustPass("W2-tcp");
    [Fact] public void W2_udp_fails() => MustPass("W2-udp");
    [Fact] public void W2_dns_fails() => MustPass("W2-dns");
    [Fact] public void W2_loopback_fails() => MustPass("W2-loopback");
    [Fact] public void W3_tiff_through_seekable_handle() => MustPass("W3-tiff");
    [Fact] public void W3_avif_through_seekable_handle() => MustPass("W3-avif");
    [Fact] public void Filesystem_outside_handles_is_denied() => MustPass("FS");
    [Fact] public void Job_memory_cap() => MustPass("JOB-mem");
    [Fact] public void Job_timeout_kills_hung_worker() => MustPass("JOB-timeout");
    [Fact] public void Job_blocks_child_processes() => MustPass("JOB-child");
    [Fact] public void Worker_dies_when_app_exits_normally() => MustPass("KILL-normal-exit");
    [Fact] public void Worker_dies_when_app_is_killed() => MustPass("KILL-process-kill");
}
