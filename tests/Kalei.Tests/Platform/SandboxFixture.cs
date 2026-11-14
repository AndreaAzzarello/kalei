using System.Runtime.Versioning;
using Kalei.Platform.Windows.Sandbox;

namespace Kalei.Tests.Platform;

/// <summary>
/// The sandbox set-up for the tests that start real Workers: a profile of the tests' own (never the one of a real Kalei) and the
/// folder access on the folder that holds the test Workers. Both are removed afterwards.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class SandboxFixture : IDisposable
{
    public SandboxFixture()
    {
        Setup = new SandboxSetup("Kalei.Test." + Guid.NewGuid().ToString("N")[..12], AppContext.BaseDirectory);
        var status = Setup.Prepare();
        if (status != SandboxSetupStatus.Ready)
        {
            throw new InvalidOperationException($"The sandbox could not be set up for the tests: {status}.");
        }
    }

    public SandboxSetup Setup { get; }

    public WindowsSandboxLauncher Launcher(string workerExe, params string[] leadingArguments) => new(Setup, workerExe, leadingArguments);

    public void Dispose() => Setup.Remove();
}
