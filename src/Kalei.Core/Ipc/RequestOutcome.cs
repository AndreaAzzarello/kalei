namespace Kalei.Core.Ipc;

/// <summary>Where a request was when it ended (for the log).</summary>
public enum IpcPhase
{
    AwaitHello,
    Open,
    Done,
}

public sealed record RequestOptions
{
    /// <summary>Total time allowed for the request: the Limits table for a Convert, <see cref="IpcLimits.ProbeTimeout"/> or <see cref="IpcLimits.PreviewTimeout"/>.</summary>
    public required TimeSpan Timeout { get; init; }

    /// <summary>Size of the Source, if known: a Progress claiming more bytes than this is refused.</summary>
    public long? SourceSize { get; init; }
}

/// <summary>How one request ended. <see cref="Reason"/> is set only when the channel failed (protocol, crash, time), not for an error the Worker reported.</summary>
public sealed record RequestOutcome
{
    public ErrorCode? Error { get; init; }

    public ProtocolReason? Reason { get; init; }

    public IpcPhase? Phase { get; init; }

    /// <summary>The Worker's answer: ProbeResult, PreviewData, Result or CanaryResult.</summary>
    public IpcMessage? Answer { get; init; }

    /// <summary>The raw BGRA pixels of a preview.</summary>
    public byte[]? Pixels { get; init; }

    public bool Succeeded => Error is null;
}
