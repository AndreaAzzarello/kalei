using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;

namespace Kalei.Core.Ipc;

/// <summary>One frame read from a Worker (or from the Core): the message and, for PreviewData, its raw pixels.</summary>
public sealed record IncomingFrame(IpcMessage Message, byte[]? Pixels = null);

/// <summary>
/// Reads and checks frames in the order of the spec: length, size cap of the type, syntax, fields, values. It allocates only
/// what the cap of the type allows, and any violation throws <see cref="IpcException"/> without anything having been used.
/// </summary>
public sealed class FrameReader(Stream stream, Direction direction, FrameRateLimiter? rate = null)
{
    private static readonly byte[] s_typePrefix = "{\"type\":\""u8.ToArray();
    private static readonly long s_origin = Stopwatch.GetTimestamp();

    private static readonly JsonDocumentOptions s_documentOptions = new()
    {
        MaxDepth = IpcLimits.MaxJsonDepth,
        AllowDuplicateProperties = false,
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
    };

    /// <summary>The next frame, or null if the stream ended cleanly between two frames.</summary>
    public async Task<IncomingFrame?> ReadAsync(CancellationToken cancellationToken = default)
    {
        var prefix = new byte[4];
        var got = await FillAsync(prefix, cancellationToken).ConfigureAwait(false);
        if (got == 0)
        {
            return null;
        }

        if (got < prefix.Length)
        {
            throw PipeClosed();
        }

        if (rate is not null && !rate.Register(Stopwatch.GetElapsedTime(s_origin)))
        {
            throw new IpcException(ProtocolReason.FrameRate);
        }

        var length = BinaryPrimitives.ReadUInt32LittleEndian(prefix);
        if (length > IpcLimits.MaxFrameBytes)
        {
            throw new IpcException(ProtocolReason.FrameTooLarge);
        }

        if (length < IpcLimits.MinFrameBytes)
        {
            throw new IpcException(ProtocolReason.BadLength);
        }

        var peek = new byte[Math.Min((int)length, IpcLimits.PeekBytes)];
        await FillExactAsync(peek, cancellationToken).ConfigureAwait(false);
        var entry = LookUp(peek);
        if (length > entry.Cap)
        {
            throw new IpcException(ProtocolReason.FrameTooLargeForType);
        }

        var body = new byte[length];
        peek.CopyTo(body, 0);
        await FillExactAsync(body.AsMemory(peek.Length), cancellationToken).ConfigureAwait(false);

        var message = Decode(body, entry);
        if (message is not PreviewDataMessage header)
        {
            return new IncomingFrame(message);
        }

        var pixels = new byte[header.Length]; // already checked: width x height x 4, at most 1 MB
        await FillExactAsync(pixels, cancellationToken).ConfigureAwait(false);
        return new IncomingFrame(message, pixels);
    }

    private MessageTable.Entry LookUp(byte[] peek)
    {
        if (peek.Length <= s_typePrefix.Length || !peek.AsSpan().StartsWith(s_typePrefix))
        {
            throw new IpcException(ProtocolReason.BadJson);
        }

        var rest = peek.AsSpan(s_typePrefix.Length);
        var end = rest.IndexOf((byte)'"');
        if (end < 0 || !MessageTable.TryGet(Encoding.ASCII.GetString(rest[..end]), direction, out var entry))
        {
            throw new IpcException(ProtocolReason.UnknownType);
        }

        return entry;
    }

    private IpcMessage Decode(byte[] body, MessageTable.Entry entry)
    {
        if (body[^1] != (byte)'}' || !Utf8.IsValid(body))
        {
            throw new IpcException(ProtocolReason.BadJson);
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body, s_documentOptions);
        }
        catch (JsonException)
        {
            throw new IpcException(ProtocolReason.BadJson);
        }

        using (document)
        {
            var root = document.RootElement;
            ShapeCheck.NoNulls(root);
            ShapeCheck.Check(root, entry.Type, topLevel: true);

            IpcMessage? message;
            try
            {
                message = root.Deserialize(IpcJsonContext.Default.IpcMessage);
            }
            catch (JsonException)
            {
                throw new IpcException(ProtocolReason.BadValue);
            }

            if (message is null || message.GetType() != entry.Type)
            {
                throw new IpcException(ProtocolReason.BadValue);
            }

            MessageValidator.Validate(message, direction);
            return message;
        }
    }

    private static IpcException PipeClosed() => new(ProtocolReason.PipeClosed, ErrorCode.Crash);

    private async Task FillExactAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        if (await FillAsync(buffer, cancellationToken).ConfigureAwait(false) < buffer.Length)
        {
            throw PipeClosed();
        }
    }

    /// <summary>Reads until the buffer is full or the stream ends; returns the number of bytes read.</summary>
    private async Task<int> FillAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var total = 0;
        try
        {
            while (total < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer[total..], cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                total += read;
            }
        }
        catch (IOException)
        {
            throw PipeClosed();
        }
        catch (ObjectDisposedException)
        {
            throw PipeClosed();
        }

        return total;
    }
}
