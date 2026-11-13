namespace Kalei.Core.Platform;

/// <summary>Where an installer or package puts files, and whether this run is portable (ADR 0010).</summary>
public interface IInstallerLayout
{
    InstallMode Mode { get; }

    /// <summary>The folder that holds the executable.</summary>
    string ApplicationDirectory { get; }
}
