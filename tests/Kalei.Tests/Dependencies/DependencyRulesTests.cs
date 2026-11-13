using Xunit;

namespace Kalei.Tests.Dependencies;

public sealed class DependencyRulesTests
{
    private static AssemblyFacts Facts(string assemblyFile) =>
        AssemblyFacts.Read(Path.Combine(AppContext.BaseDirectory, assemblyFile));

    // --- the scanner itself: the fixture breaks every rule, so every rule must fire -----------------------------------

    [Fact]
    public void Scanner_FindsEveryForbiddenUseInTheFixture()
    {
        var facts = Facts("Kalei.Tests.dll");

        var coreViolations = DependencyRules.Core.Check(facts);

        Assert.Contains(coreViolations, v => v.Contains("System.Net.Http.HttpClient", StringComparison.Ordinal));
        Assert.Contains(coreViolations, v => v.Contains("System.Diagnostics.Process", StringComparison.Ordinal));
        Assert.Contains(coreViolations, v => v.Contains("System.IO.Pipes", StringComparison.Ordinal));
        Assert.Contains(coreViolations, v => v.Contains("Microsoft.Win32.Registry", StringComparison.Ordinal));
        Assert.Contains(coreViolations, v => v.Contains("native call", StringComparison.Ordinal));
    }

    [Fact]
    public void CoreRule_AllowsSafeHandles()
    {
        var coreViolations = DependencyRules.Core.Check(Facts("Kalei.Tests.dll"));

        Assert.DoesNotContain(coreViolations, v => v.Contains("SafeHandles", StringComparison.Ordinal));
    }

    [Fact]
    public void AppRule_FlagsNetworkTypesButNotProcessOrPipes()
    {
        var appViolations = DependencyRules.App.Check(Facts("Kalei.Tests.dll"));

        Assert.Contains(appViolations, v => v.Contains("System.Net.Http.HttpClient", StringComparison.Ordinal));
        Assert.DoesNotContain(appViolations, v => v.Contains("System.Diagnostics.Process", StringComparison.Ordinal));
    }

    [Fact]
    public void DonutRule_FlagsOperatingSystemApis()
    {
        var donutViolations = DependencyRules.Donut.Check(Facts("Kalei.Tests.dll"));

        Assert.Contains(donutViolations, v => v.Contains("Microsoft.Win32.Registry", StringComparison.Ordinal));
        Assert.Contains(donutViolations, v => v.Contains("native call", StringComparison.Ordinal));
    }

    // --- the real assemblies -------------------------------------------------------------------------------------------

    [Fact]
    public void Core_HasNoForbiddenDependency() =>
        Assert.Empty(DependencyRules.Core.Check(Facts("Kalei.Core.dll")));

    [Fact]
    public void App_DoesNotReferenceSystemNet() =>
        Assert.Empty(DependencyRules.App.Check(Facts("Kalei.App.dll")));

    [Fact]
    public void Donut_ReferencesNoUiOrOperatingSystemApi() =>
        Assert.Empty(DependencyRules.Donut.Check(Facts("Kalei.Donut.dll")));
}
