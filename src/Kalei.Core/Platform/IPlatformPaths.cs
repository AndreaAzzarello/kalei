namespace Kalei.Core.Platform;

/// <summary>Data, log, settings and temporary folders (ADR 0010, settings.md). Only the Core's own folders; never a user path.</summary>
public interface IPlatformPaths
{
    string DataFolder { get; }

    string LogFolder { get; }

    string SettingsFile { get; }

    string TempFolder { get; }

    /// <summary>Creates the data folder if needed and proves that a file can be written there; leaves nothing behind.</summary>
    DataFolderStatus CheckDataFolder();
}
