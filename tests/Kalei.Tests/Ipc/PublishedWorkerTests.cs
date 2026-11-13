using System.Diagnostics;
using System.Runtime.Versioning;
using Kalei.Core.Ipc;
using Xunit;

namespace Kalei.Tests.Ipc;

/// <summary>
/// The Worker as it ships: published trimmed and self-contained. Proves it speaks the protocol and measures the time to its Hello,
/// the figure that the 5 s handshake timeout of docs/ipc-spec.md is checked against.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class PublishedWorkerTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "kalei-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try
        {
            Directory.Delete(_folder, recursive: true);
        }
        catch (IOException)
        {
            // a scanner may hold a file for a moment; the system temp folder is cleaned anyway
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string RepositoryRoot()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "Kalei.slnx")))
        {
            folder = folder.Parent;
        }

        return folder?.FullName ?? throw new InvalidOperationException("Kalei.slnx not found above the test folder.");
    }

    private async Task<string> PublishAsync()
    {
        var info = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var argument in new[]
        {
            "publish", Path.Combine(RepositoryRoot(), "src", "Kalei.Worker.Builtin"), "-c", "Release", "-r", "win-x64", "--self-contained",
            "-p:TreatWarningsAsErrors=true", "-o", _folder,
        })
        {
            info.ArgumentList.Add(argument);
        }

        using var publish = Process.Start(info)!;
        var output = publish.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var errors = publish.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        await publish.WaitForExitAsync(TestContext.Current.CancellationToken);
        Assert.True(publish.ExitCode == 0, "dotnet publish failed:\n" + await output + await errors);
        return Path.Combine(_folder, "Kalei.Worker.Builtin.exe");
    }

    /// <summary>Process creation to the Hello frame fully read, in milliseconds.</summary>
    private static async Task<double> TimeToHelloAsync(string exe)
    {
        var launcher = new ProcessLauncher(exe);
        var clock = Stopwatch.StartNew();
        using var worker = launcher.Start();
        var frame = await new FrameReader(worker.FromWorker, Direction.ToCore).ReadAsync(TestContext.Current.CancellationToken);
        var elapsed = clock.Elapsed.TotalMilliseconds;
        Assert.IsType<HelloMessage>(frame!.Message);
        return elapsed;
    }

    [Fact]
    public async Task PublishedTrimmedWorker_SpeaksTheProtocol_AndSaysHelloWellWithinTheHandshakeTimeout_WarmOnThisPc_RepeatColdOnTheVmInTicket07b()
    {
        var exe = await PublishAsync();

        // Real session over real pipes against the shipped binary.
        var session = new WorkerSession(new ProcessLauncher(exe), new BuiltinWorkerTests.BuiltinCatalog(), new BuiltinWorkerTests.NoLog(), WatchdogTimes.Default);
        var outcome = await session.RunAsync(new ProbeMessage { Source = 4 }, new RequestOptions { Timeout = TimeSpan.FromSeconds(30) }, null,
            TestContext.Current.CancellationToken);
        Assert.Equal(ErrorCode.Format, outcome.Error);
        Assert.Null(outcome.Reason);

        // One start to bring the files into the cache, then ten measured starts: WARM, not a cold start.
        await TimeToHelloAsync(exe);
        var times = new List<double>();
        for (var i = 0; i < 10; i++)
        {
            times.Add(await TimeToHelloAsync(exe));
        }

        times.Sort();
        var median = (times[4] + times[5]) / 2;
        var report =
            $"Published trimmed Worker, process creation to Hello, WARM on this PC (10 starts): min {times[0]:F0} ms, median {median:F0} ms, max {times[^1]:F0} ms. "
            + "Not a cold start: repeat cold on the reference VM in ticket 07b (handshake timeout 5000 ms).";
        TestContext.Current.SendDiagnosticMessage(report);
        await File.WriteAllTextAsync(Path.Combine(AppContext.BaseDirectory, "worker-hello-latency.txt"), report, TestContext.Current.CancellationToken);

        Assert.True(times[^1] < WatchdogTimes.Default.Hello.TotalMilliseconds, $"slowest Hello {times[^1]:F0} ms against a 5000 ms timeout");
    }
}
