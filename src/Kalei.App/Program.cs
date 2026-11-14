using Avalonia;
using Kalei.Platform.Windows;
using Kalei.Platform.Windows.Sandbox;

namespace Kalei.App;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        // The clean-up command runs before anything else and opens no window (Windows only: the sandbox adapters of
        // the other systems come with their own spikes, ADR 0010).
        if (OperatingSystem.IsWindows()
            && SandboxCommandLine.TryRun(args, SandboxSetup.ForBuiltinWorker(new WindowsInstallerLayout(AppContext.BaseDirectory).ApplicationDirectory)) is { } exitCode)
        {
            return exitCode;
        }

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect();
}
