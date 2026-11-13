using System.Xml.Linq;
using Xunit;

namespace Kalei.Tests.Build;

/// <summary>
/// Guards the decision in docs/TRD.md ("Culture and globalization"): the App needs culture data for Italian formatting,
/// and no switch is set for the whole solution. The Worker's own setting is allowed by the rule, not checked here.
/// </summary>
public sealed class GlobalizationSettingsTests
{
    private static string RepoFile(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Kalei.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return Path.Combine([dir.FullName, .. parts]);
    }

    private static string? Setting(string file, string name) =>
        XDocument.Load(file).Descendants(name).Select(e => e.Value.Trim()).LastOrDefault();

    [Fact]
    public void SolutionWideProps_DoNotSetInvariantGlobalization() =>
        Assert.Null(Setting(RepoFile("Directory.Build.props"), "InvariantGlobalization"));

    [Fact]
    public void App_KeepsCultureData() =>
        Assert.Equal("false", Setting(RepoFile("src", "Kalei.App", "Kalei.App.csproj"), "InvariantGlobalization"));

    [Theory]
    [InlineData("Kalei.Core")]
    [InlineData("Kalei.Donut")]
    public void CoreAndDonut_SetNoGlobalizationSwitch(string project) =>
        Assert.Null(Setting(RepoFile("src", project, project + ".csproj"), "InvariantGlobalization"));
}
