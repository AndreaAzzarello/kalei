namespace Kalei.Core.Detection;

/// <summary>Reads the start of a file for the header stage, never more than <see cref="HeaderDetector.MaxHeaderBytes"/> (S3).</summary>
public static class HeaderReader
{
    /// <summary>Fills <paramref name="buffer"/> (at most 64 KB of it) from the stream; returns how many bytes were read, 0 for an empty stream.</summary>
    public static int Read(Stream source, Span<byte> buffer)
    {
        var limit = Math.Min(buffer.Length, HeaderDetector.MaxHeaderBytes);
        var total = 0;
        while (total < limit)
        {
            var n = source.Read(buffer[total..limit]);
            if (n <= 0)
            {
                break;
            }

            total += n;
        }

        return total;
    }
}
