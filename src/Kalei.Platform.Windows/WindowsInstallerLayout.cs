using Kalei.Core.Platform;

namespace Kalei.Platform.Windows;

/// <summary>Portable when a <c>kalei.portable</c> marker file sits next to the executable (settings.md).</summary>
public sealed class WindowsInstallerLayout : IInstallerLayout
{
    public const string PortableMarker = "kalei.portable";

    public WindowsInstallerLayout(string applicationDirectory)
    {
        ApplicationDirectory = applicationDirectory;
        Mode = File.Exists(Path.Combine(applicationDirectory, PortableMarker)) ? InstallMode.Portable : InstallMode.Installed;
    }

    public InstallMode Mode { get; }

    public string ApplicationDirectory { get; }
}
