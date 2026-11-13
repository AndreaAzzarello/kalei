using System.Runtime.Versioning;
using Kalei.Core.Platform;
using Kalei.Platform.Windows;
using Xunit;

namespace Kalei.Tests.Platform;

[SupportedOSPlatform("windows")]
public sealed class WindowsPathsTests
{
    private const string Marker = "kalei.portable";

    // --- installer layout: installed or portable ---------------------------------------------------------------------

    [Fact]
    public void Layout_WithoutMarker_IsInstalled()
    {
        using var app = new TempFolder();

        var layout = new WindowsInstallerLayout(app.Path);

        Assert.Equal(InstallMode.Installed, layout.Mode);
        Assert.Equal(app.Path, layout.ApplicationDirectory);
    }

    [Fact]
    public void Layout_WithMarkerNextToTheExecutable_IsPortable()
    {
        using var app = new TempFolder();
        File.WriteAllText(app.Combine(Marker), string.Empty);

        var layout = new WindowsInstallerLayout(app.Path);

        Assert.Equal(InstallMode.Portable, layout.Mode);
    }

    // --- paths -------------------------------------------------------------------------------------------------------

    [Fact]
    public void Installed_UsesTheKaleiFolderInLocalAppData()
    {
        using var app = new TempFolder();
        using var local = new TempFolder();

        var paths = new WindowsPlatformPaths(new WindowsInstallerLayout(app.Path), local.Path);

        var root = local.Combine("Kalei");
        Assert.Equal(root, paths.DataFolder);
        Assert.Equal(Path.Combine(root, "logs"), paths.LogFolder);
        Assert.Equal(Path.Combine(root, "settings.json"), paths.SettingsFile);
        Assert.Equal(Path.Combine(root, "temp"), paths.TempFolder);
    }

    [Fact]
    public void Portable_UsesADataFolderNextToTheExecutable()
    {
        using var app = new TempFolder();
        using var local = new TempFolder();
        File.WriteAllText(app.Combine(Marker), string.Empty);

        var paths = new WindowsPlatformPaths(new WindowsInstallerLayout(app.Path), local.Path);

        var root = app.Combine("data");
        Assert.Equal(root, paths.DataFolder);
        Assert.Equal(Path.Combine(root, "logs"), paths.LogFolder);
        Assert.Equal(Path.Combine(root, "settings.json"), paths.SettingsFile);
        Assert.Equal(Path.Combine(root, "temp"), paths.TempFolder);
        Assert.False(Directory.Exists(local.Combine("Kalei")), "portable mode must not touch LocalAppData");
    }

    // --- writability -------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CheckDataFolder_WritableFolder_IsWritableAndLeavesNothingBehind(bool portable)
    {
        using var app = new TempFolder();
        using var local = new TempFolder();
        if (portable)
        {
            File.WriteAllText(app.Combine(Marker), string.Empty);
        }

        var paths = new WindowsPlatformPaths(new WindowsInstallerLayout(app.Path), local.Path);

        Assert.Equal(DataFolderStatus.Writable, paths.CheckDataFolder());
        Assert.Empty(Directory.GetFileSystemEntries(paths.DataFolder));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CheckDataFolder_WhenTheFolderCannotBeCreated_IsNotWritable(bool portable)
    {
        using var app = new TempFolder();
        using var local = new TempFolder();
        if (portable)
        {
            File.WriteAllText(app.Combine(Marker), string.Empty);
            File.WriteAllText(app.Combine("data"), "a file where the folder should be");
        }
        else
        {
            File.WriteAllText(local.Combine("Kalei"), "a file where the folder should be");
        }

        var paths = new WindowsPlatformPaths(new WindowsInstallerLayout(app.Path), local.Path);

        Assert.Equal(DataFolderStatus.NotWritable, paths.CheckDataFolder());
    }
}
