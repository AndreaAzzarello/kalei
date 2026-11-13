using Kalei.Core.Ipc;

namespace Kalei.Worker.Builtin;

/// <summary>The Modules this Worker executable hosts, as it announces them in Hello. A constant of the build (spec: sent before any engine loads).</summary>
internal static class BuiltinModules
{
    public static IReadOnlyList<ModuleInfo> All { get; } =
    [
        new ModuleInfo { Id = "images", Version = "1.0.0" },
        new ModuleInfo { Id = "data", Version = "1.0.0" },
        new ModuleInfo { Id = "text", Version = "1.0.0" },
    ];
}
