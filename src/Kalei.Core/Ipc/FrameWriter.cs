using System.Buffers.Binary;
using System.Text.Json;

namespace Kalei.Core.Ipc;

/// <summary>Writes frames: a 4-byte little-endian length, then the compact JSON body whose first property is "type".</summary>
public static class FrameWriter
{
    public static byte[] Encode(IpcMessage message) => JsonSerializer.SerializeToUtf8Bytes(message, IpcJsonContext.Default.IpcMessage);

    /// <summary>A message over its cap is a bug of the sender, not a protocol event: nothing is written.</summary>
    public static async Task WriteAsync(Stream stream, IpcMessage message, CancellationToken cancellationToken = default)
    {
        var frame = Build(message, 0);
        await stream.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>The header frame of a preview, then the raw BGRA pixels, in one write.</summary>
    public static async Task WritePreviewDataAsync(Stream stream, int width, int height, ReadOnlyMemory<byte> pixels,
        CancellationToken cancellationToken = default)
    {
        var header = new PreviewDataMessage { Width = width, Height = height, Length = pixels.Length };
        var frame = Build(header, pixels.Length);
        pixels.CopyTo(frame.AsMemory(frame.Length - pixels.Length));
        await stream.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static byte[] Build(IpcMessage message, int trailing)
    {
        var body = Encode(message);
        var cap = MessageTable.LargestCapOf(message.GetType());
        if (body.Length > cap)
        {
            throw new InvalidOperationException($"{message.GetType().Name} is {body.Length} bytes, over its cap of {cap}.");
        }

        var frame = new byte[4 + body.Length + trailing];
        BinaryPrimitives.WriteUInt32LittleEndian(frame, (uint)body.Length);
        body.CopyTo(frame, 4);
        return frame;
    }
}
