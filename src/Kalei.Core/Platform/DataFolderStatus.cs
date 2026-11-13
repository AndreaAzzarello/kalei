namespace Kalei.Core.Platform;

/// <summary>Whether Kalei can keep its journal, logs, settings and temporary files (S7).</summary>
public enum DataFolderStatus
{
    Writable,
    NotWritable,
}
