using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kalei.Core.Ipc;

/// <summary>A message of docs/ipc-spec.md v1. The discriminator is the first JSON property, "type".</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(HelloMessage), "Hello")]
[JsonDerivedType(typeof(ProbeMessage), "Probe")]
[JsonDerivedType(typeof(ProbeResultMessage), "ProbeResult")]
[JsonDerivedType(typeof(PreviewMessage), "Preview")]
[JsonDerivedType(typeof(PreviewDataMessage), "PreviewData")]
[JsonDerivedType(typeof(ConvertMessage), "Convert")]
[JsonDerivedType(typeof(ProgressMessage), "Progress")]
[JsonDerivedType(typeof(ResultMessage), "Result")]
[JsonDerivedType(typeof(ErrorMessage), "Error")]
[JsonDerivedType(typeof(CancelMessage), "Cancel")]
[JsonDerivedType(typeof(CanaryMessage), "Canary")]
[JsonDerivedType(typeof(CanaryResultMessage), "CanaryResult")]
public abstract record IpcMessage;

public sealed record ModuleInfo
{
    public required string Id { get; init; }

    public required string Version { get; init; }
}

/// <summary>Worker to Core: Worker and Modules are required; Core to Worker: only Protocol.</summary>
public sealed record HelloMessage : IpcMessage
{
    public required int Protocol { get; init; }

    public string? Worker { get; init; }

    public IReadOnlyList<ModuleInfo>? Modules { get; init; }
}

public sealed record ProbeMessage : IpcMessage
{
    public required int Source { get; init; }

    public string? Candidate { get; init; }
}

public sealed record ProbeProperties
{
    public int? Width { get; init; }

    public int? Height { get; init; }

    public int? Frames { get; init; }

    public bool? HasAlpha { get; init; }

    public int? Sheets { get; init; }

    public string? Encoding { get; init; }
}

public sealed record ProbeResultMessage : IpcMessage
{
    public required string Format { get; init; }

    public required Confidence Confidence { get; init; }

    public required ProbeProperties Properties { get; init; }
}

public sealed record PreviewMessage : IpcMessage
{
    public required int Source { get; init; }

    public required int MaxSize { get; init; }
}

/// <summary>Header of a preview; Length raw BGRA bytes follow it on the pipe.</summary>
public sealed record PreviewDataMessage : IpcMessage
{
    public required int Width { get; init; }

    public required int Height { get; init; }

    public required int Length { get; init; }
}

public sealed record ConvertLimits
{
    public required long Pixels { get; init; }

    public required int Depth { get; init; }

    public required int YamlAliases { get; init; }

    public required int ExpansionRatio { get; init; }

    public required long UncompressedBytes { get; init; }
}

public sealed record ConvertMessage : IpcMessage
{
    public required string From { get; init; }

    public required string To { get; init; }

    public required int Source { get; init; }

    public required int Output { get; init; }

    public required Dictionary<string, JsonElement> Options { get; init; }

    public required ConvertLimits Limits { get; init; }
}

/// <summary>With neither field it is a pure heartbeat.</summary>
public sealed record ProgressMessage : IpcMessage
{
    public double? Fraction { get; init; }

    public long? Bytes { get; init; }
}

public sealed record ResultMessage : IpcMessage
{
    public required long OutputBytes { get; init; }

    public IReadOnlyList<Notice>? Warnings { get; init; }
}

/// <summary>No free text, by design: S10 holds by construction (spec, decision D3).</summary>
public sealed record ErrorMessage : IpcMessage
{
    public required ErrorCode Code { get; init; }
}

public sealed record CancelMessage : IpcMessage;

public sealed record CanaryMessage : IpcMessage
{
    public required int LocalPort { get; init; }
}

public sealed record CanaryProbe
{
    public required CanaryOutcome Outcome { get; init; }

    public int? Code { get; init; }
}

public sealed record CanaryResultMessage : IpcMessage
{
    public required CanaryProbe Local { get; init; }

    public required CanaryProbe Net { get; init; }
}
