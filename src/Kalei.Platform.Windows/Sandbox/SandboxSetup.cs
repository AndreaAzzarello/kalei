using System.Security.AccessControl;
using System.Security.Principal;

namespace Kalei.Platform.Windows.Sandbox;

public enum SandboxSetupStatus
{
    Ready,

    /// <summary>The Worker's folder gives the sandbox no access and is not the user's own: nothing was changed, and no conversion can start.</summary>
    FolderNotOwned,

    Failed,
}

/// <summary>
/// First-run set-up and clean-up of the sandbox (ticket 07b, criterion 5): the AppContainer profile and read-and-execute access
/// to the Worker's folder for ALL APPLICATION PACKAGES, so the AppContainer can load its own files (W4). Nothing else is granted.
/// </summary>
public sealed class SandboxSetup(string profileName, string workerFolder, SecurityIdentifier? currentUser = null)
{
    private static readonly SecurityIdentifier s_allApplicationPackages = new("S-1-15-2-1");

    public string ProfileName { get; } = profileName;

    public static SandboxSetup ForBuiltinWorker(string workerFolder) => new(AppContainerProfile.BuiltinWorker, workerFolder);

    public SandboxSetupStatus Prepare()
    {
        try
        {
            AppContainerProfile.Ensure(ProfileName).Dispose();

            var folder = new DirectoryInfo(workerFolder);
            var security = folder.GetAccessControl();
            if (HasGrant(security))
            {
                return SandboxSetupStatus.Ready;
            }

            var user = currentUser ?? WindowsIdentity.GetCurrent().User;
            if (user is null || security.GetOwner(typeof(SecurityIdentifier)) != user)
            {
                return SandboxSetupStatus.FolderNotOwned;
            }

            security.AddAccessRule(new FileSystemAccessRule(s_allApplicationPackages, FileSystemRights.ReadAndExecute,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            folder.SetAccessControl(security);
            return SandboxSetupStatus.Ready;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException or InvalidOperationException)
        {
            return SandboxSetupStatus.Failed;
        }
    }

    /// <summary>Removes the profile and the grant this set-up made. True if both are gone; safe to repeat.</summary>
    public bool Remove()
    {
        var removed = AppContainerProfile.Delete(ProfileName);
        try
        {
            var folder = new DirectoryInfo(workerFolder);
            if (folder.Exists)
            {
                var security = folder.GetAccessControl();
                security.RemoveAccessRuleAll(new FileSystemAccessRule(s_allApplicationPackages, FileSystemRights.ReadAndExecute, AccessControlType.Allow));
                folder.SetAccessControl(security);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            removed = false;
        }

        return removed;
    }

    private static bool HasGrant(DirectorySecurity security) => security
        .GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier))
        .Cast<FileSystemAccessRule>()
        .Any(r => r.AccessControlType == AccessControlType.Allow
            && r.IdentityReference == s_allApplicationPackages
            && (r.FileSystemRights & FileSystemRights.ReadAndExecute) == FileSystemRights.ReadAndExecute
            && r.InheritanceFlags.HasFlag(InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit));
}
