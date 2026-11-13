namespace Kalei.Core.Ipc;

/// <summary>What the Core knows from the loaded manifests, used to check what a Worker claims. The manifest loader is a later ticket.</summary>
public interface IModuleCatalog
{
    /// <summary>True if a manifest of this Worker executable has this Module id and version.</summary>
    bool HasModule(string id, string version);

    /// <summary>True if a loaded manifest declares this Format.</summary>
    bool HasFormat(string id);
}
