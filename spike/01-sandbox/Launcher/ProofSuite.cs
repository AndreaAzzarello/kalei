using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.Json;
using NetVips;

namespace SandboxSpike.Launcher;

public sealed record ProofResult(string Id, string Name, string Status, string Detail, Dictionary<string, object?>? Numbers = null)
{
    public bool Passed => Status == "PASS";
}

public static class InstallFolder
{
    public static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KaleiSpike");
    public static string Worker => Path.Combine(Root, "worker");
    public static string Work => Path.Combine(Root, "work");
    public static string Canary => Path.Combine(Root, "canary");

    public static void CopyWorker(string source)
    {
        if (Directory.Exists(Worker)) Directory.Delete(Worker, true);
        foreach (var f in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(Worker, Path.GetRelativePath(source, f));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(f, target, true);
        }
    }

    /// <summary>Read and execute for a well-known group, no admin needed (the folder is owned by the user).</summary>
    public static int Grant(string sid)
    {
        var psi = new ProcessStartInfo("icacls.exe", $"\"{Worker}\" /grant \"*{sid}:(OI)(CI)RX\" /T /C /Q")
        { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        using var p = Process.Start(psi)!;
        p.StandardOutput.ReadToEnd();
        p.StandardError.ReadToEnd();
        p.WaitForExit();
        return p.ExitCode;
    }

    public static void Cleanup()
    {
        try { if (Directory.Exists(Root)) Directory.Delete(Root, true); } catch { /* best effort, reported by the caller */ }
    }
}

/// <summary>All sandbox proofs of ticket 01. Each negative proof (something must FAIL) has a positive control outside the sandbox.</summary>
public sealed class ProofSuite(string workerSource)
{
    private const string AllApplicationPackages = "S-1-15-2-1";
    private const string AllRestrictedApplicationPackages = "S-1-15-2-2";
    public List<ProofResult> Results { get; } = [];
    private string W => InstallFolder.Worker;

    private void Add(string id, string name, string status, string detail, Dictionary<string, object?>? numbers = null)
    {
        Results.Add(new ProofResult(id, name, status, detail, numbers));
        Console.WriteLine($"[{status,-12}] {id,-10} {name} — {detail}");
    }

    private RunResult Run(Isolation iso, string mode, string? source = null, string? output = null, string[]? extra = null, int timeoutMs = 60_000, bool oneProcess = true) =>
        SandboxProcess.Run(new RunOptions
        {
            Isolation = iso, Mode = mode, WorkerDir = W, SourcePath = source, OutputPath = output,
            ExtraArgs = extra ?? [], TimeoutMs = timeoutMs, LimitToOneProcess = oneProcess,
        });

    private static string Describe(RunResult r) =>
        !r.Started ? $"did not start (Win32 error {r.StartError})"
        : !r.Reported ? $"no report (exit 0x{r.ExitCode:X}, timeout={r.TimedOut})"
        : r.WorkerOk ? "worked" : "failed: " + r.Error;

    public void RunAll()
    {
        NetVips.Cache.Max = 0; // the operation cache would keep verified output files open and block the final clean-up
        InstallFolder.Cleanup();
        Directory.CreateDirectory(InstallFolder.Work);
        Directory.CreateDirectory(InstallFolder.Canary);
        File.WriteAllText(Path.Combine(InstallFolder.Canary, "secret.txt"), "canary");
        try
        {
            Conditions();
            var png = MakeInputs(out var tiff);
            InstallAndW4();
            W1(png);
            W3(tiff);
            W2();
            Fs(png);
            Job();
            KillOnClose();
            Measure(png);
        }
        finally
        {
            SandboxProcess.DeleteProfile();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            InstallFolder.Cleanup();
            if (Directory.Exists(InstallFolder.Root)) Console.WriteLine("WARNING: could not remove " + InstallFolder.Root);
        }
    }

    // ---- Conditions (read only) --------------------------------------------------------------------------------
    private void Conditions()
    {
        string Cmd(string exe, string args)
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo(exe, args) { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true })!;
                var o = p.StandardOutput.ReadToEnd();
                p.WaitForExit(10_000);
                return o;
            }
            catch (Exception e) { return "n/a " + e.GetType().Name; }
        }
        // Language independent: PowerShell objects, not localised tool output.
        var ps = Cmd("powershell.exe", "-NoProfile -Command \"(Get-Service mpssvc,BFE | % { $_.Name + '=' + $_.Status }) -join ','; "
            + "(Get-NetFirewallProfile | % { $_.Name + '=' + $_.Enabled }) -join ','\"").Trim().Replace("\r", "").Replace("\n", "; ");
        var exemptions = Cmd("CheckNetIsolation", "LoopbackExempt -s").Split('\n').Count(l => l.Contains("S-1-15-2-"));
        var ours = Cmd("CheckNetIsolation", "LoopbackExempt -s").Contains("Kalei.Spike");
        Add("ENV", "Machine and conditions (read only)", "INFO",
            $"{RuntimeInformation.OSDescription}; {Environment.ProcessorCount} logical CPUs; .NET {Environment.Version}; services and firewall profiles: {ps}; "
            + $"loopback exemptions on this PC: {exemptions} (none for the spike profile: {!ours}); third-party firewall or antivirus not inspected");
    }

    // ---- Inputs ------------------------------------------------------------------------------------------------
    private static string MakeInputs(out string tiff)
    {
        var png = Path.Combine(InstallFolder.Work, "in.png");
        Image Gradient(int w, int h)
        {
            var xyz = Image.Xyz(w, h);
            var r = (xyz[0] * (255.0 / w)).Cast(Enums.BandFormat.Uchar);
            var g = (xyz[1] * (255.0 / h)).Cast(Enums.BandFormat.Uchar);
            var b = ((xyz[0] + xyz[1]) * (255.0 / (w + h))).Cast(Enums.BandFormat.Uchar);
            return r.Bandjoin(g, b).Copy(interpretation: Enums.Interpretation.Srgb);
        }
        using (var img = Gradient(1600, 1200)) img.WriteToFile(png);
        tiff = Path.Combine(InstallFolder.Work, "pyramid.tif");
        using (var big = Gradient(4096, 4096))
            big.WriteToFile(tiff, new VOption { { "tile", true }, { "pyramid", true }, { "compression", Enums.ForeignTiffCompression.Deflate }, { "tile_width", 128 }, { "tile_height", 128 } });
        return png;
    }

    // ---- W4 (and the ACL that makes it work) -------------------------------------------------------------------
    private void InstallAndW4()
    {
        InstallFolder.CopyWorker(workerSource);
        var before = Run(Isolation.AppContainer, "info", timeoutMs: 20_000);
        Add("W4-control", "AppContainer cannot start the Worker before the ACL grant", before.Reported && before.WorkerOk ? "INCONCLUSIVE" : "INFO",
            "before grant: " + Describe(before) + " (informative: shows the ACL is what enables the launch)");

        var acl = InstallFolder.Grant(AllApplicationPackages);
        var after = Run(Isolation.AppContainer, "info", timeoutMs: 30_000);
        var ok = after.Reported && after.WorkerOk;
        Add("W4", "Worker, .NET runtime and libvips load from a per-user folder after ACL grant, no admin", ok ? "PASS" : "FAIL",
            $"icacls exit {acl}; {Describe(after)}; libvips {after.Get<string>("libvips")}; runtime {after.Get<string>("runtime")}");
    }

    private bool Verify(string path, string fmt, out string detail)
    {
        try
        {
            using var img = Image.NewFromFile(path);
            var avg = img.Avg();
            detail = $"{fmt} {new FileInfo(path).Length} bytes, {img.Width}x{img.Height}, avg {avg:F1}";
            return img.Width == 1600 && img.Height == 1200 && avg > 10;
        }
        catch (Exception e) { detail = e.GetType().Name + ": " + e.Message; return false; }
    }

    // ---- W1 ----------------------------------------------------------------------------------------------------
    private string? _avifOut;
    private void W1(string png)
    {
        foreach (var fmt in new[] { "avif", "webp" })
        {
            var control = Path.Combine(InstallFolder.Work, $"control.{fmt}");
            var c = Run(Isolation.None, "convert", png, control, [$"--fmt {fmt}"]);
            var outPath = Path.Combine(InstallFolder.Work, $"out.{fmt}");
            var r = Run(Isolation.AppContainer, "convert", png, outPath, [$"--fmt {fmt}"]);
            var detail = "no output file";
            var verified = File.Exists(outPath) && Verify(outPath, fmt, out detail);
            var status = !(c.Reported && c.WorkerOk) ? "INCONCLUSIVE" : r.WorkerOk && verified ? "PASS" : "FAIL";
            Add($"W1-{fmt}", $"PNG to {fmt.ToUpper()} in AppContainer + Job from inherited handles", status,
                $"control {Describe(c)}; sandbox {Describe(r)}; output checked by the launcher: {detail}",
                new() { ["convertMs"] = r.Get<long>("convertMs"), ["hello"] = r.HelloMs });
            if (fmt == "avif" && verified) _avifOut = outPath;

            var lpacOut = Path.Combine(InstallFolder.Work, $"lpac.{fmt}");
            var restrictedAcl = InstallFolder.Grant(AllRestrictedApplicationPackages);
            var l = Run(Isolation.Lpac, "convert", png, lpacOut, [$"--fmt {fmt}"]);
            Add($"W1-{fmt}-lpac", $"Same conversion in LPAC (variant for the matrix)", "INFO", $"icacls {restrictedAcl}; {Describe(l)}");
        }
    }

    // ---- W3 ----------------------------------------------------------------------------------------------------
    private void W3(string tiff)
    {
        int pages;
        using (var t = Image.NewFromFile(tiff)) pages = Convert.ToInt32(t.Get("n-pages"));
        var c = Run(Isolation.None, "read", tiff, extra: [$"--pages {pages}"]);
        var r = Run(Isolation.AppContainer, "read", tiff, extra: [$"--pages {pages}"]);
        var status = !(c.Reported && c.WorkerOk) ? "INCONCLUSIVE" : r.WorkerOk && r.Get<bool>("canSeek") && r.Get<int>("seeks") > 0 ? "PASS" : "FAIL";
        Add("W3-tiff", "Seek-heavy tiled pyramid TIFF read through an inherited handle", status,
            $"control {Describe(c)}; sandbox {Describe(r)}; canSeek={r.Get<bool>("canSeek")}; seeks={r.Get<int>("seeks")}; regions={r.Get<int>("regions")}; levels={r.Get<string>("levels")} ({pages} pages)");

        if (_avifOut is null) { Add("W3-avif", "AVIF read through an inherited handle", "INCONCLUSIVE", "no AVIF produced by W1"); return; }
        var ca = Run(Isolation.None, "read", _avifOut);
        var ra = Run(Isolation.AppContainer, "read", _avifOut);
        Add("W3-avif", "AVIF read through an inherited handle", !(ca.Reported && ca.WorkerOk) ? "INCONCLUSIVE" : ra.WorkerOk && ra.Get<bool>("canSeek") ? "PASS" : "FAIL",
            $"control {Describe(ca)}; sandbox {Describe(ra)}; canSeek={ra.Get<bool>("canSeek")}; seeks={ra.Get<int>("seeks")}");
    }

    // ---- W2 ----------------------------------------------------------------------------------------------------
    private void W2()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var probes = new (string Id, string Name, string Mode, string[] Args)[]
        {
            ("W2-tcp", "TCP connect to 1.1.1.1:443", "net-tcp", ["--host 1.1.1.1", "--port 443"]),
            ("W2-udp", "UDP DNS query to 1.1.1.1:53", "net-udp", []),
            ("W2-dns", "DNS lookup of example.com through the system resolver", "net-dns", []),
            ("W2-loopback", "TCP connect to a listener on 127.0.0.1", "net-loopback", ["--host 127.0.0.1", $"--port {port}"]),
        };
        foreach (var (id, name, mode, args) in probes)
        {
            var c = Run(Isolation.None, mode, extra: args, timeoutMs: 20_000);
            var ac = Run(Isolation.AppContainer, mode, extra: args, timeoutMs: 20_000);
            string status;
            if (!(c.Reported && c.WorkerOk)) status = "INCONCLUSIVE";      // the control must succeed outside the sandbox
            else if (!ac.Reported) status = "INCONCLUSIVE";                // the Worker must run and report its own failure
            else status = ac.WorkerOk ? "FAIL" : "PASS";
            Add(id, name + " must FAIL in the sandbox", status, $"control (no sandbox) {Describe(c)}; AppContainer {Describe(ac)}");

            var lp = Run(Isolation.Lpac, mode, extra: args, timeoutMs: 20_000);
            Add(id + "-lpac", name + " (LPAC variant)", "INFO", Describe(lp));
        }
        listener.Stop();
    }

    // ---- Filesystem outside the handles ------------------------------------------------------------------------
    private void Fs(string png)
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] probes =
        [
            $"--probe readfile={Path.Combine(InstallFolder.Canary, "secret.txt")}",
            $"--probe readfile={png}",
            $"--probe listdir={profile}",
            $"--probe writefile={Path.GetTempPath().TrimEnd('\\')}",
            $"--probe writefile={W}",
            $"--probe writefile={profile}",
        ];
        var c = Run(Isolation.None, "fs-probe", extra: probes);
        var r = Run(Isolation.AppContainer, "fs-probe", extra: probes);
        var cAllowed = Allowed(c);
        var rAllowed = Allowed(r);
        var status = !(c.Reported && c.WorkerOk) || cAllowed.Any(x => !x) ? "INCONCLUSIVE"
            : !(r.Reported && r.WorkerOk) ? "INCONCLUSIVE"
            : rAllowed.Any(x => x) ? "FAIL" : "PASS";
        Add("FS", "Access by path outside the handles is denied (canary file, Source by path, profile listing, writes to temp, install folder and profile)",
            status, $"control allowed {cAllowed.Count(x => x)}/{cAllowed.Count}; AppContainer allowed {rAllowed.Count(x => x)}/{rAllowed.Count}: "
                    + string.Join(", ", ProbeNames(r)));
    }

    private static List<bool> Allowed(RunResult r)
    {
        var list = new List<bool>();
        if (r.Result is null || !r.Result.TryGetValue("probes", out var p)) return list;
        foreach (var e in p.EnumerateArray()) list.Add(e.GetProperty("allowed").GetBoolean());
        return list;
    }

    private static IEnumerable<string> ProbeNames(RunResult r)
    {
        if (r.Result is null || !r.Result.TryGetValue("probes", out var p)) yield break;
        foreach (var e in p.EnumerateArray())
            yield return $"{e.GetProperty("kind").GetString()}:{(e.GetProperty("allowed").GetBoolean() ? "ALLOWED" : "denied")}";
    }

    // ---- Job Object ----------------------------------------------------------------------------------------------
    private void Job()
    {
        var a = Run(Isolation.AppContainer, "alloc", timeoutMs: 90_000);
        var peakMb = a.PeakMemoryBytes / 1024 / 1024;
        var reachedCap = a.Reported && a.Get<string>("stoppedBy") == "cap";
        Add("JOB-mem", "Memory cap of 2 GB stops an allocating Worker", !reachedCap && peakMb <= 2200 && a.Started ? "PASS" : "FAIL",
            $"{Describe(a)}; allocatedMb={a.Get<int>("allocatedMb")}; stoppedBy={a.Get<string>("stoppedBy")}; exit 0x{a.ExitCode:X}; job peak {peakMb} MB");

        var h = Run(Isolation.AppContainer, "hang", timeoutMs: 4000);
        Add("JOB-timeout", "A hung Worker is killed at the timeout", h.TimedOut && h.TotalMs < 12_000 ? "PASS" : "FAIL",
            $"timedOut={h.TimedOut}; waited {h.TotalMs:F0} ms; exit {h.ExitCode}");

        var c = Run(Isolation.None, "spawn-child", oneProcess: false, timeoutMs: 20_000);
        var s = Run(Isolation.AppContainer, "spawn-child", oneProcess: true, timeoutMs: 20_000);
        var denied = !(s.Reported && s.WorkerOk);
        Add("JOB-child", "A Worker cannot start child processes (Job limit of 1 active process)", !(c.Reported && c.WorkerOk) ? "INCONCLUSIVE" : s.Started && denied ? "PASS" : "FAIL",
            $"control (no job limit) {Describe(c)}; sandbox {Describe(s)}");
    }

    // ---- Kill when the app closes ---------------------------------------------------------------------------------
    private void KillOnClose()
    {
        var host = Path.ChangeExtension(typeof(ProofSuite).Assembly.Location, ".exe");
        if (!File.Exists(host)) host = Environment.ProcessPath!;
        foreach (var how in new[] { "normal-exit", "process-kill" })
        {
            var psi = new ProcessStartInfo(host, $"host-hang --worker \"{W}\"")
            { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, CreateNoWindow = true };
            using var app = Process.Start(psi)!;
            var line = app.StandardOutput.ReadLine() ?? "";
            if (!line.StartsWith("PID ")) { Add("KILL-" + how, "Worker dies when the app closes", "FAIL", "host said: " + line); app.Kill(); continue; }
            var pid = int.Parse(line[4..]);
            var aliveBefore = IsAlive(pid);
            if (how == "normal-exit") { app.StandardInput.WriteLine("exit"); app.WaitForExit(10_000); }
            else { app.Kill(); app.WaitForExit(10_000); }
            var sw = Stopwatch.StartNew();
            while (IsAlive(pid) && sw.ElapsedMilliseconds < 2000) Thread.Sleep(20);
            var goneMs = sw.ElapsedMilliseconds;
            Add("KILL-" + how, $"Worker dies within 2 s when the app closes ({how})", aliveBefore && !IsAlive(pid) ? "PASS" : "FAIL",
                $"alive before={aliveBefore}; gone after {goneMs} ms");
            if (IsAlive(pid)) try { Process.GetProcessById(pid).Kill(); } catch { /* already gone */ }
        }
    }

    private static bool IsAlive(int pid)
    {
        try { using var p = Process.GetProcessById(pid); return !p.HasExited; }
        catch (ArgumentException) { return false; }
    }

    // ---- Measures (median of 10) ------------------------------------------------------------------------------------
    private void Measure(string png)
    {
        var hello = new List<double>(); var convert = new List<double>(); var total = new List<double>(); var peak = new List<double>();
        for (var i = 0; i < 10; i++)
        {
            var o = Path.Combine(InstallFolder.Work, $"m{i}.avif");
            var r = Run(Isolation.AppContainer, "convert", png, o, ["--fmt avif"]);
            if (!r.WorkerOk) { Add("MEASURE", "Run " + i, "FAIL", Describe(r)); return; }
            hello.Add(r.HelloMs); convert.Add(r.Get<long>("convertMs")); total.Add(r.TotalMs); peak.Add(r.PeakMemoryBytes / 1024.0 / 1024.0);
        }
        double Med(List<double> l) { var s = l.OrderBy(x => x).ToList(); return (s[4] + s[5]) / 2; }
        Add("MEASURE", "Median of 10 runs, 1600x1200 PNG to AVIF in AppContainer + Job (warm file cache, not a cold start)", "INFO",
            $"first message {Med(hello):F0} ms; conversion {Med(convert):F0} ms; process total {Med(total):F0} ms; peak private memory {Med(peak):F0} MB",
            new() { ["helloMs"] = Med(hello), ["convertMs"] = Med(convert), ["totalMs"] = Med(total), ["peakMb"] = Med(peak) });
    }

    // ---- Report ----------------------------------------------------------------------------------------------------
    public void WriteReport(string path)
    {
        var user = Environment.UserName;
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var report = new
        {
            machine = "Windows 11 dev PC — to repeat on the reference VM (Hyper-V Win10 22H2, 2 vCPU, 4 GB)",
            os = RuntimeInformation.OSDescription,
            dotnet = Environment.Version.ToString(),
            date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            results = Results.Select(r => new { r.Id, r.Name, r.Status, r.Detail, r.Numbers }),
        };
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        json = json.Replace(profile.Replace("\\", "\\\\"), "%USERPROFILE%").Replace(user, "<user>");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, json);
    }
}
