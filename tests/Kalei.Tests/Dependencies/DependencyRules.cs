namespace Kalei.Tests.Dependencies;

/// <summary>
/// The dependency rules of ticket 05 (architecture: no OS API in the Core, no network in the app).
/// Each rule lists forbidden type prefixes and assembly prefixes; an exception lets a sub-namespace through.
/// </summary>
internal sealed class DependencyRules
{
    private readonly string[] _forbiddenTypePrefixes;
    private readonly string[] _allowedTypePrefixes;
    private readonly string[] _forbiddenAssemblyPrefixes;
    private readonly bool _forbidNativeCalls;

    private DependencyRules(
        string[] forbiddenTypePrefixes,
        string[] allowedTypePrefixes,
        string[] forbiddenAssemblyPrefixes,
        bool forbidNativeCalls)
    {
        _forbiddenTypePrefixes = forbiddenTypePrefixes;
        _allowedTypePrefixes = allowedTypePrefixes;
        _forbiddenAssemblyPrefixes = forbiddenAssemblyPrefixes;
        _forbidNativeCalls = forbidNativeCalls;
    }

    /// <summary>
    /// Core: no System.Net.*, Process, Pipes, Microsoft.Win32.* or native call. Microsoft.Win32.SafeHandles is allowed:
    /// it is a cross-platform type that the Broker needs for file handles (ticket 08).
    /// </summary>
    public static DependencyRules Core { get; } = new(
        forbiddenTypePrefixes: ["System.Net.", "System.Diagnostics.Process", "System.IO.Pipes.", "Microsoft.Win32."],
        allowedTypePrefixes: ["Microsoft.Win32.SafeHandles."],
        forbiddenAssemblyPrefixes: ["System.Net.", "System.Diagnostics.Process", "System.IO.Pipes", "Microsoft.Win32.Registry"],
        forbidNativeCalls: true);

    /// <summary>App: no System.Net.* (S5, S6: no outbound network call).</summary>
    public static DependencyRules App { get; } = new(
        forbiddenTypePrefixes: ["System.Net."],
        allowedTypePrefixes: [],
        forbiddenAssemblyPrefixes: ["System.Net."],
        forbidNativeCalls: false);

    /// <summary>Donut: pure library, no UI, no operating-system API, no other project of the solution.</summary>
    public static DependencyRules Donut { get; } = new(
        forbiddenTypePrefixes:
        [
            "Avalonia.", "System.Windows.", "System.Net.", "System.Diagnostics.Process", "System.IO.Pipes.", "Microsoft.Win32.",
        ],
        allowedTypePrefixes: [],
        forbiddenAssemblyPrefixes:
        [
            "Avalonia", "Kalei.", "WindowsBase", "PresentationFramework", "System.Net.", "System.Diagnostics.Process",
            "System.IO.Pipes", "Microsoft.Win32.Registry",
        ],
        forbidNativeCalls: true);

    public IReadOnlyList<string> Check(AssemblyFacts facts)
    {
        var violations = new List<string>();

        foreach (var type in facts.ReferencedTypes.Order(StringComparer.Ordinal))
        {
            var forbidden = _forbiddenTypePrefixes.Any(p => type.StartsWith(p, StringComparison.Ordinal))
                && !_allowedTypePrefixes.Any(p => type.StartsWith(p, StringComparison.Ordinal));
            if (forbidden)
            {
                violations.Add($"references type {type}");
            }
        }

        foreach (var assembly in facts.ReferencedAssemblies.Order(StringComparer.Ordinal))
        {
            if (_forbiddenAssemblyPrefixes.Any(p => assembly.StartsWith(p, StringComparison.Ordinal)))
            {
                violations.Add($"references assembly {assembly}");
            }
        }

        if (_forbidNativeCalls)
        {
            violations.AddRange(facts.NativeCalls.Select(m => $"declares a native call: {m}"));
        }

        return violations;
    }
}
