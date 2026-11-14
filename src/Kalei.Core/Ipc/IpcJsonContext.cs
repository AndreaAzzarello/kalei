using System.Text.Json.Serialization;

namespace Kalei.Core.Ipc;

/// <summary>
/// Source-generated metadata (trimming safe, no reflection). Unknown members are refused and nulls are refused for non-nullable
/// members; duplicate keys, nesting depth and nulls are checked on the parsed document before this context is used.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    RespectNullableAnnotations = true)]
[JsonSerializable(typeof(IpcMessage))]
internal sealed partial class IpcJsonContext : JsonSerializerContext;
