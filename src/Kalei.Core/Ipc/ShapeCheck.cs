using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Kalei.Core.Ipc;

/// <summary>
/// Tells an unknown field and a missing field apart from a bad value, at any depth, using the generated metadata
/// (the exception text of the serializer is not a stable contract, so it is never parsed).
/// </summary>
internal static class ShapeCheck
{
    public static void NoNulls(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
                throw new IpcException(ProtocolReason.BadJson);
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    NoNulls(property.Value);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    NoNulls(item);
                }

                break;
        }
    }

    /// <param name="element">A JSON value.</param>
    /// <param name="type">The type it must have.</param>
    /// <param name="topLevel">True for the message itself, whose "type" member is the discriminator.</param>
    public static void Check(JsonElement element, Type type, bool topLevel)
    {
        if (element.ValueKind == JsonValueKind.Array && type.IsGenericType)
        {
            var itemType = type.GetGenericArguments()[0];
            foreach (var item in element.EnumerateArray())
            {
                Check(item, itemType, topLevel: false);
            }

            return;
        }

        var info = IpcJsonContext.Default.GetTypeInfo(type);
        if (info is not { Kind: JsonTypeInfoKind.Object } || element.ValueKind != JsonValueKind.Object)
        {
            return; // a scalar, a dictionary or the wrong shape: the typed pass reports it as a bad value
        }

        var known = info.Properties.ToDictionary(p => p.Name, StringComparer.Ordinal);
        foreach (var member in element.EnumerateObject())
        {
            if (topLevel && member.Name == "type")
            {
                continue;
            }

            if (!known.ContainsKey(member.Name))
            {
                throw new IpcException(ProtocolReason.UnknownField);
            }
        }

        foreach (var property in info.Properties)
        {
            if (property.IsRequired && !element.TryGetProperty(property.Name, out _))
            {
                throw new IpcException(ProtocolReason.MissingField);
            }
        }

        foreach (var member in element.EnumerateObject())
        {
            if (known.TryGetValue(member.Name, out var property))
            {
                Check(member.Value, property.PropertyType, topLevel: false);
            }
        }
    }
}
