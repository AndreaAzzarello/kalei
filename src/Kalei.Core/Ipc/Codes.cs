using System.Text.Json.Serialization;

namespace Kalei.Core.Ipc;

/// <summary>Enum written as its member name, never as a number.</summary>
internal sealed class StrictEnumConverter<T>() : JsonStringEnumConverter<T>(null, allowIntegerValues: false)
    where T : struct, Enum;

/// <summary>Error catalogue of the spec. The wire names are the E_* codes.</summary>
[JsonConverter(typeof(StrictEnumConverter<ErrorCode>))]
public enum ErrorCode
{
    [JsonStringEnumMemberName("E_FORMAT")] Format,
    [JsonStringEnumMemberName("E_CORRUPT")] Corrupt,
    [JsonStringEnumMemberName("E_SIZE")] Size,
    [JsonStringEnumMemberName("E_PIXELS")] Pixels,
    [JsonStringEnumMemberName("E_COUNT")] Count,
    [JsonStringEnumMemberName("E_DEPTH")] Depth,
    [JsonStringEnumMemberName("E_MEM")] Mem,
    [JsonStringEnumMemberName("E_TIME")] Time,
    [JsonStringEnumMemberName("E_BOMB")] Bomb,
    [JsonStringEnumMemberName("E_COMPLEX")] Complex,
    [JsonStringEnumMemberName("E_UNSUPPORTED_OPTION")] UnsupportedOption,
    [JsonStringEnumMemberName("E_NO_CODEC")] NoCodec,
    [JsonStringEnumMemberName("E_SANDBOX")] Sandbox,
    [JsonStringEnumMemberName("E_PROTOCOL")] Protocol,
    [JsonStringEnumMemberName("E_CRASH")] Crash,
    [JsonStringEnumMemberName("E_CANCELLED")] Cancelled,
    [JsonStringEnumMemberName("E_IO")] Io,
}

public static class ErrorCodes
{
    /// <summary>The only codes a Worker may send (spec, "Error codes"); every other code is raised by the Core.</summary>
    public static bool IsWorkerSendable(this ErrorCode code) => code is ErrorCode.Format or ErrorCode.Corrupt or ErrorCode.Pixels
        or ErrorCode.Mem or ErrorCode.Bomb or ErrorCode.Complex or ErrorCode.UnsupportedOption or ErrorCode.NoCodec or ErrorCode.Io;
}

/// <summary>The only notices a Worker may send; the Core raises the others.</summary>
[JsonConverter(typeof(StrictEnumConverter<Notice>))]
public enum Notice
{
    [JsonStringEnumMemberName("FIRST_FRAME_ONLY")] FirstFrameOnly,
    [JsonStringEnumMemberName("LOSSY_ENCODING")] LossyEncoding,
}

[JsonConverter(typeof(StrictEnumConverter<Confidence>))]
public enum Confidence
{
    /// <summary>The structure was read and is valid.</summary>
    [JsonStringEnumMemberName("certain")] Certain,

    /// <summary>Signature and header agree, but the structure was not read in full.</summary>
    [JsonStringEnumMemberName("likely")] Likely,
}

[JsonConverter(typeof(StrictEnumConverter<CanaryOutcome>))]
public enum CanaryOutcome
{
    [JsonStringEnumMemberName("connected")] Connected,
    [JsonStringEnumMemberName("timeout")] Timeout,
    [JsonStringEnumMemberName("error")] Error,
}

/// <summary>Why a request ended badly: the closed list the log carries (never frame content, S10).</summary>
public enum ProtocolReason
{
    FrameTooLarge,
    BadLength,
    FrameTooLargeForType,
    BadJson,
    UnknownType,
    UnknownField,
    MissingField,
    BadValue,
    OutOfSequence,
    BadVersion,
    FrameRate,
    PipeClosed,
    HelloTimeout,
    Heartbeat,
    Timeout,
}

/// <summary>A frame broke the protocol (or the pipe closed): the Worker is ended with <see cref="Code"/>.</summary>
public sealed class IpcException(ProtocolReason reason, ErrorCode code = ErrorCode.Protocol) : Exception(reason.ToString())
{
    public ProtocolReason Reason { get; } = reason;

    public ErrorCode Code { get; } = code;
}
