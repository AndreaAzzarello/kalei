using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using Kalei.Platform.Windows.Sandbox;
using Xunit;

namespace Kalei.Tests.Platform;

[SupportedOSPlatform("windows")]
public sealed class SandboxCommandLineTests
{
    [Fact]
    public void RemoveSandbox_RemovesTheProfileAndTheGrant_AndExitsWithZero()
    {
        using var folder = new TempFolder();
        var setup = new SandboxSetup("Kalei.Test." + Guid.NewGuid().ToString("N")[..12], folder.Path);
        Assert.Equal(SandboxSetupStatus.Ready, setup.Prepare());

        var exitCode = SandboxCommandLine.TryRun(["--remove-sandbox"], setup);

        Assert.Equal(0, exitCode);
        var grants = new DirectoryInfo(folder.Path).GetAccessControl().GetAccessRules(true, false, typeof(SecurityIdentifier))
            .Cast<FileSystemAccessRule>().Count(r => r.IdentityReference == new SecurityIdentifier("S-1-15-2-1"));
        Assert.Equal(0, grants);
    }

    [Theory]
    [InlineData]
    [InlineData("--other")]
    [InlineData("--remove-sandbox", "extra")]
    [InlineData("file.png")]
    public void AnythingElse_IsNotACommand_AndTheAppStartsNormally(params string[] args)
    {
        using var folder = new TempFolder();
        var setup = new SandboxSetup("Kalei.Test." + Guid.NewGuid().ToString("N")[..12], folder.Path);

        Assert.Null(SandboxCommandLine.TryRun(args, setup));
    }
}
