namespace Kalei.Platform.Windows.Sandbox;

/// <summary>
/// The clean-up command of the app (release-pipeline.md, "Sandbox set-up at install time"): <c>Kalei.App.exe --remove-sandbox</c>
/// removes the AppContainer profile and the folder access. No window; the exit code says how it went. The uninstaller calls it,
/// and the README tells the user to run it before deleting a portable folder.
/// </summary>
public static class SandboxCommandLine
{
    public const string RemoveSandbox = "--remove-sandbox";

    /// <summary>The exit code if the arguments are the clean-up command, otherwise null and the app starts normally.</summary>
    public static int? TryRun(string[] args, SandboxSetup setup) => args is [RemoveSandbox] ? (setup.Remove() ? 0 : 1) : null;
}
