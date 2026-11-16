using Kalei.Core.Detection;
using Xunit;

namespace Kalei.Tests.Detection;

/// <summary>Untrusted input: the reader stays within 64 KB and the detector never throws (S2, S3).</summary>
public sealed class HeaderReaderAndRobustnessTests
{
    private static readonly string?[] Allowed = [null, "png", "jpg", "gif", "webp", "bmp", "tiff", "ico", "avif", "heic"];

    /// <summary>A stream of a declared (possibly huge) length that holds no memory and counts what was read.</summary>
    private sealed class SyntheticStream(long length, int maxPerRead = int.MaxValue) : Stream
    {
        public long BytesRead { get; private set; }

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            var n = (int)Math.Min(Math.Min(buffer.Length, maxPerRead), length - BytesRead);
            buffer[..n].Clear();
            BytesRead += n;
            return n;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => length;

        public override long Position { get => BytesRead; set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [Fact]
    public void A_huge_file_is_read_up_to_64_KB_only()
    {
        using var s = new SyntheticStream(200L * 1024 * 1024 * 1024);
        var buffer = new byte[HeaderDetector.MaxHeaderBytes + 4096];
        Assert.Equal(HeaderDetector.MaxHeaderBytes, HeaderReader.Read(s, buffer));
        Assert.Equal(HeaderDetector.MaxHeaderBytes, s.BytesRead);
    }

    [Fact]
    public void A_smaller_buffer_limits_the_read()
    {
        using var s = new SyntheticStream(1_000_000);
        Assert.Equal(100, HeaderReader.Read(s, new byte[100]));
        Assert.Equal(100, s.BytesRead);
    }

    [Fact]
    public void Partial_reads_are_completed()
    {
        using var s = new SyntheticStream(1_000_000, maxPerRead: 1);
        Assert.Equal(HeaderDetector.MaxHeaderBytes, HeaderReader.Read(s, new byte[HeaderDetector.MaxHeaderBytes]));
    }

    [Fact]
    public void An_empty_or_short_stream_gives_what_there_is()
    {
        using var empty = new SyntheticStream(0);
        Assert.Equal(0, HeaderReader.Read(empty, new byte[64]));
        using var short3 = new SyntheticStream(3);
        Assert.Equal(3, HeaderReader.Read(short3, new byte[64]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(HeaderDetector.MaxHeaderBytes)]
    [InlineData(HeaderDetector.MaxHeaderBytes + 1)]
    [InlineData(1_048_576)]
    public void Zero_and_ff_filled_inputs_of_any_length_are_handled(int length)
    {
        Assert.Contains(HeaderDetector.Detect(new byte[length]), Allowed);
        Assert.Contains(HeaderDetector.Detect(Enumerable.Repeat((byte)0xFF, length).ToArray()), Allowed);
    }

    /// <summary>Fixed seed and iteration count: the same inputs on every run.</summary>
    [Fact]
    public void Fuzz_10000_inputs_never_throw()
    {
        const int Iterations = 10_000;
        var rng = new Random(0x06_0006);
        byte[][] prefixes =
        [
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
            [0xFF, 0xD8, 0xFF],
            "GIF89a"u8.ToArray(),
            "RIFF\0\0\0\0WEBP"u8.ToArray(),
            [0x49, 0x49, 0x2A, 0x00],
            [0x42, 0x4D],
            [0, 0, 1, 0],
            [0, 0, 0, 0x20, (byte)'f', (byte)'t', (byte)'y', (byte)'p'],
        ];
        uint[] sizes = [0, 1, 7, 15, 16, 20, 0x7FFFFFFF, uint.MaxValue];

        for (var i = 0; i < Iterations; i++)
        {
            var data = new byte[rng.Next(0, 70_001)];
            rng.NextBytes(data);
            if (i % 2 == 0)
            {
                var prefix = prefixes[rng.Next(prefixes.Length)];
                prefix.AsSpan(0, Math.Min(prefix.Length, data.Length)).CopyTo(data);
                if (prefix.Length == 8 && prefix[4] == (byte)'f' && data.Length >= 8)
                {
                    System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(data, sizes[rng.Next(sizes.Length)]);
                }
            }

            Assert.Contains(HeaderDetector.Detect(data), Allowed);
            _ = FormatDetection.Classify(data, i % 3 == 0 ? ".png" : null);
        }
    }
}
