using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using Kalei.Platform.Windows.Sandbox;
using Microsoft.Win32;
using Xunit;

namespace Kalei.Tests.Platform;

[SupportedOSPlatform("windows")]
public sealed class SandboxSetupTests
{
    private static readonly SecurityIdentifier s_allApplicationPackages = new("S-1-15-2-1");

    // A profile name of its own, so the tests never touch the one of a real Kalei on this PC.
    private static string NewProfileName() => "Kalei.Test." + Guid.NewGuid().ToString("N")[..12];

    private static bool ProfileExists(string name)
    {
        // Profiles are listed by SID in the user's registry hive; the name is the Moniker value.
        using var mappings = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppContainer\Mappings");
        return mappings is not null && mappings.GetSubKeyNames().Any(sid =>
        {
            using var key = mappings.OpenSubKey(sid);
            return string.Equals(key?.GetValue("Moniker") as string, name, StringComparison.OrdinalIgnoreCase)
                || (key?.GetValue("Moniker") as string)?.StartsWith(name + "_", StringComparison.OrdinalIgnoreCase) == true;
        });
    }

    private static int ExplicitGrants(string folder) => new DirectoryInfo(folder).GetAccessControl()
        .GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier))
        .Cast<FileSystemAccessRule>()
        .Count(r => r.IdentityReference == s_allApplicationPackages && r.AccessControlType == AccessControlType.Allow);

    [Fact]
    public void Prepare_OnAFolderTheUserOwns_CreatesTheProfileAndGrantsReadAndExecute()
    {
        using var folder = new TempFolder();
        var name = NewProfileName();
        var setup = new SandboxSetup(name, folder.Path);
        try
        {
            Assert.Equal(SandboxSetupStatus.Ready, setup.Prepare());

            Assert.True(ProfileExists(name));
            var rule = Assert.Single(new DirectoryInfo(folder.Path).GetAccessControl()
                .GetAccessRules(true, false, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>(), r => r.IdentityReference == s_allApplicationPackages);
            Assert.Equal(FileSystemRights.ReadAndExecute | FileSystemRights.Synchronize, rule.FileSystemRights);
            Assert.Equal(InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, rule.InheritanceFlags);
        }
        finally
        {
            setup.Remove();
        }
    }

    [Fact]
    public void Prepare_Twice_IsReadyAndDoesNotAddASecondGrant()
    {
        using var folder = new TempFolder();
        var setup = new SandboxSetup(NewProfileName(), folder.Path);
        try
        {
            Assert.Equal(SandboxSetupStatus.Ready, setup.Prepare());
            Assert.Equal(SandboxSetupStatus.Ready, setup.Prepare());

            Assert.Equal(1, ExplicitGrants(folder.Path));
        }
        finally
        {
            setup.Remove();
        }
    }

    [Fact]
    public void Remove_TakesAwayTheProfileAndTheGrant_AndIsSafeToRepeat()
    {
        using var folder = new TempFolder();
        var name = NewProfileName();
        var setup = new SandboxSetup(name, folder.Path);
        setup.Prepare();

        Assert.True(setup.Remove());
        Assert.False(ProfileExists(name));
        Assert.Equal(0, ExplicitGrants(folder.Path));
        Assert.True(setup.Remove());
    }

    [Fact]
    public void Prepare_WhenTheGrantIsMissingAndTheFolderIsNotTheUsers_ChangesNothing()
    {
        using var folder = new TempFolder();
        var name = NewProfileName();
        var someoneElse = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
        var setup = new SandboxSetup(name, folder.Path, someoneElse);
        try
        {
            Assert.Equal(SandboxSetupStatus.FolderNotOwned, setup.Prepare());

            Assert.Equal(0, ExplicitGrants(folder.Path));
        }
        finally
        {
            setup.Remove();
        }
    }

    [Fact]
    public void Prepare_WhenTheInstallerAlreadyGrantedAccess_IsReadyEvenIfTheFolderIsNotTheUsers()
    {
        using var folder = new TempFolder();
        var info = new DirectoryInfo(folder.Path);
        var security = info.GetAccessControl();
        security.AddAccessRule(new FileSystemAccessRule(s_allApplicationPackages, FileSystemRights.ReadAndExecute,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        info.SetAccessControl(security);
        var setup = new SandboxSetup(NewProfileName(), folder.Path, new SecurityIdentifier(WellKnownSidType.WorldSid, null));
        try
        {
            Assert.Equal(SandboxSetupStatus.Ready, setup.Prepare());
        }
        finally
        {
            setup.Remove();
        }
    }

    [Fact]
    public void Prepare_OnAFolderThatDoesNotExist_Fails()
    {
        var setup = new SandboxSetup(NewProfileName(), Path.Combine(Path.GetTempPath(), "kalei-no-such-" + Guid.NewGuid().ToString("N")));
        try
        {
            Assert.Equal(SandboxSetupStatus.Failed, setup.Prepare());
        }
        finally
        {
            setup.Remove();
        }
    }
}
