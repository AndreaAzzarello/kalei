using Kalei.Core.Platform;

namespace Kalei.Platform.Windows;

/// <summary>
/// Installed: <c>%LOCALAPPDATA%\Kalei</c>. Portable: a <c>data</c> folder next to the executable.
/// The local application data root is passed in so tests never touch the real one.
/// </summary>
public sealed class WindowsPlatformPaths : IPlatformPaths
{
    public WindowsPlatformPaths(IInstallerLayout layout, string localAppDataRoot)
    {
        DataFolder = layout.Mode == InstallMode.Portable
            ? Path.Combine(layout.ApplicationDirectory, "data")
            : Path.Combine(localAppDataRoot, "Kalei");
        LogFolder = Path.Combine(DataFolder, "logs");
        SettingsFile = Path.Combine(DataFolder, "settings.json");
        TempFolder = Path.Combine(DataFolder, "temp");
    }

    public string DataFolder { get; }

    public string LogFolder { get; }

    public string SettingsFile { get; }

    public string TempFolder { get; }

    public static WindowsPlatformPaths ForCurrentUser(IInstallerLayout layout) =>
        new(layout, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

    public DataFolderStatus CheckDataFolder()
    {
        try
        {
            Directory.CreateDirectory(DataFolder);
            var probe = Path.Combine(DataFolder, $".write-check-{Guid.NewGuid():N}");
            using (new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose))
            {
            }

            return DataFolderStatus.Writable;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return DataFolderStatus.NotWritable;
        }
    }
}
