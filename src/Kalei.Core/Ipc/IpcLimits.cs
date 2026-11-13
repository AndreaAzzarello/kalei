namespace Kalei.Core.Ipc;

/// <summary>Constants of docs/ipc-spec.md v1. The caps per message type are in <see cref="MessageTable"/>.</summary>
public static class IpcLimits
{
    public const int ProtocolVersion = 1;

    /// <summary>Protocol maximum for the length of a frame (1 MB).</summary>
    public const int MaxFrameBytes = 1_048_576;

    /// <summary>The shortest valid body: <c>{"type":"Cancel"}</c>.</summary>
    public const int MinFrameBytes = 17;

    /// <summary>Bytes read first, to learn the type and apply its cap before reading the rest.</summary>
    public const int PeekBytes = 32;

    public const int MaxJsonDepth = 4;
    public const int MaxFramesPerSecond = 100;
    public const int MaxPreviewSize = 512;
    public const int MinPreviewSize = 16;
    public const int MaxOptions = 32;
    public const int MaxOptionStringBytes = 256;
    public const int MaxModules = 16;

    public static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan PreviewTimeout = TimeSpan.FromSeconds(30);
}
