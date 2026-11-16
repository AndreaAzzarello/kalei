using System.Buffers.Binary;
using System.Text;
using Kalei.Core.Detection;
using Xunit;

namespace Kalei.Tests.Detection;

/// <summary>Header stage of docs/format-detection.md: signature table and the ftyp brand rule.</summary>
public sealed class HeaderDetectorTests
{
    private static byte[] Bytes(params byte[] b) => b;

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    /// <summary>An ISO-BMFF file that starts with one ftyp box: size, "ftyp", major brand, minor version, compatible brands.</summary>
    internal static byte[] Ftyp(string major, params string[] compatible)
    {
        var box = new byte[16 + 4 * compatible.Length];
        BinaryPrimitives.WriteUInt32BigEndian(box, (uint)box.Length);
        Ascii("ftyp").CopyTo(box, 4);
        Ascii(major).CopyTo(box, 8);
        for (var i = 0; i < compatible.Length; i++)
        {
            Ascii(compatible[i]).CopyTo(box, 16 + 4 * i);
        }

        return box;
    }

    // --- simple signatures ---------------------------------------------------------------------------------------------

    public static TheoryData<string, byte[]> Valid() => new()
    {
        { "png", Bytes(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A) },
        { "jpg", Bytes(0xFF, 0xD8, 0xFF) },
        { "gif", Ascii("GIF87a") },
        { "gif", Ascii("GIF89a") },
        { "webp", [.. Ascii("RIFF"), 0, 0, 0, 0, .. Ascii("WEBP")] },
        { "tiff", Bytes(0x49, 0x49, 0x2A, 0x00) },
        { "tiff", Bytes(0x4D, 0x4D, 0x00, 0x2A) },
        { "bmp", Bmp(40) },
        { "ico", Bytes(0, 0, 1, 0, 1, 0) },
        { "ico", Bytes(0, 0, 1, 0, 255, 0) },
    };

    private static byte[] Bmp(uint headerSize)
    {
        var b = new byte[18];
        b[0] = (byte)'B';
        b[1] = (byte)'M';
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(14), headerSize);
        return b;
    }

    [Theory]
    [MemberData(nameof(Valid))]
    public void Signature_gives_the_format(string expected, byte[] header) =>
        Assert.Equal(expected, HeaderDetector.Detect(header));

    [Theory]
    [MemberData(nameof(Valid))]
    public void One_byte_less_is_unknown(string _, byte[] header)
    {
        // The shortest valid header of every row, minus its last byte, must not match (BMP and ICO need their full length).
        if (header.Length > 1)
        {
            Assert.Null(HeaderDetector.Detect(header.AsSpan(0, header.Length - 1)));
        }
    }

    [Theory]
    [InlineData(12)]
    [InlineData(40)]
    [InlineData(52)]
    [InlineData(56)]
    [InlineData(108)]
    [InlineData(124)]
    public void Bmp_accepts_the_listed_header_sizes(uint size) => Assert.Equal("bmp", HeaderDetector.Detect(Bmp(size)));

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(64)]
    [InlineData(uint.MaxValue)]
    public void Bmp_rejects_other_header_sizes(uint size) => Assert.Null(HeaderDetector.Detect(Bmp(size)));

    [Theory]
    [InlineData(0)]
    [InlineData(256)]
    public void Ico_count_must_be_1_to_255(ushort count) =>
        Assert.Null(HeaderDetector.Detect(Bytes(0, 0, 1, 0, (byte)(count & 0xFF), (byte)(count >> 8))));

    [Fact]
    public void Cur_is_not_supported() => Assert.Null(HeaderDetector.Detect(Bytes(0, 0, 2, 0, 1, 0)));

    [Fact]
    public void Riff_without_webp_is_unknown() =>
        Assert.Null(HeaderDetector.Detect([.. Ascii("RIFF"), 0, 0, 0, 0, .. Ascii("WAVE")]));

    [Fact]
    public void Text_and_zip_are_unknown_here()
    {
        Assert.Null(HeaderDetector.Detect(Ascii("hello world")));
        Assert.Null(HeaderDetector.Detect(Bytes(0x50, 0x4B, 0x03, 0x04)));
        Assert.Null(HeaderDetector.Detect([]));
    }

    // --- ftyp: AVIF and HEIC -------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("avif", "avif")]
    [InlineData("avis", "avif")]
    [InlineData("heic", "heic")]
    [InlineData("heix", "heic")]
    [InlineData("hevc", "heic")]
    [InlineData("hevx", "heic")]
    public void Known_major_brand_decides(string major, string expected) =>
        Assert.Equal(expected, HeaderDetector.Detect(Ftyp(major)));

    [Theory]
    [InlineData("mif1")]
    [InlineData("msf1")]
    public void Generic_major_looks_at_compatible_brands(string major)
    {
        Assert.Equal("avif", HeaderDetector.Detect(Ftyp(major, "miaf", "avif")));
        Assert.Equal("heic", HeaderDetector.Detect(Ftyp(major, "miaf", "heic")));
        Assert.Null(HeaderDetector.Detect(Ftyp(major, "miaf", "iso8")));
        Assert.Null(HeaderDetector.Detect(Ftyp(major)));
    }

    // The conflict rule (docs/format-detection.md): the major brand wins when it is one of the six known brands; only with a
    // generic major (mif1, msf1) are the compatible brands read, and if both avif and heic are there, AVIF wins, whatever their order.
    [Theory]
    [InlineData("avif", "heic", "avif")]
    [InlineData("avis", "heic", "avif")]
    [InlineData("avif", "hevc", "avif")]
    [InlineData("heic", "avif", "heic")]
    [InlineData("heix", "avif", "heic")]
    [InlineData("hevc", "avis", "heic")]
    public void Major_brand_wins_over_a_conflicting_compatible_brand(string major, string compatible, string expected) =>
        Assert.Equal(expected, HeaderDetector.Detect(Ftyp(major, compatible)));

    [Theory]
    [InlineData("mif1")]
    [InlineData("msf1")]
    public void Avif_wins_when_a_generic_major_has_both_in_any_order(string major)
    {
        Assert.Equal("avif", HeaderDetector.Detect(Ftyp(major, "avif", "heic")));
        Assert.Equal("avif", HeaderDetector.Detect(Ftyp(major, "heic", "avif")));
        Assert.Equal("avif", HeaderDetector.Detect(Ftyp(major, "heic", "miaf", "avif", "heic")));
    }

    [Theory]
    [InlineData("heis")] // listed neither in the table nor in the rule: unknown
    [InlineData("isom")]
    [InlineData("mp42")]
    [InlineData("qt  ")]
    public void Other_major_brands_are_unknown_even_with_a_known_compatible_brand(string major) =>
        Assert.Null(HeaderDetector.Detect(Ftyp(major, "avif", "heic")));

    [Fact]
    public void Sequence_brands_are_not_compatible_brands_of_the_rule() =>
        Assert.Null(HeaderDetector.Detect(Ftyp("mif1", "avis", "heis")));

    // --- ftyp: hostile box sizes ---------------------------------------------------------------------------------------

    private static byte[] FtypWithSize(uint size, string major, params string[] compatible)
    {
        var box = Ftyp(major, compatible);
        BinaryPrimitives.WriteUInt32BigEndian(box, size);
        return box;
    }

    [Fact]
    public void Box_size_zero_means_up_to_the_end_of_the_data() =>
        Assert.Equal("avif", HeaderDetector.Detect(FtypWithSize(0, "mif1", "heic", "avif")));

    [Theory]
    [InlineData(1u)]
    [InlineData(7u)]
    [InlineData(15u)]
    public void Box_size_too_small_is_unknown(uint size) =>
        Assert.Null(HeaderDetector.Detect(FtypWithSize(size, "avif")));

    [Fact]
    public void Box_size_beyond_the_data_only_reads_what_is_there() =>
        Assert.Equal("avif", HeaderDetector.Detect(FtypWithSize(uint.MaxValue, "mif1", "heic", "avif")));

    [Fact]
    public void Box_size_cuts_the_compatible_brands()
    {
        // The box ends after "heic": the later "avif" is not inside the ftyp box, so it does not count.
        Assert.Equal("heic", HeaderDetector.Detect(FtypWithSize(20, "mif1", "heic", "avif")));
    }

    [Fact]
    public void Largesize_box_reads_brands_after_the_64_bit_size()
    {
        // size = 1 -> a 64-bit size at offset 8, the major brand moves to 16, minor version 20, compatible brands from 24.
        var b = new byte[32];
        BinaryPrimitives.WriteUInt32BigEndian(b, 1);
        Ascii("ftyp").CopyTo(b, 4);
        BinaryPrimitives.WriteUInt64BigEndian(b.AsSpan(8), 32);
        Ascii("mif1").CopyTo(b, 16);
        Ascii("heic").CopyTo(b, 24);
        Ascii("avif").CopyTo(b, 28);
        Assert.Equal("avif", HeaderDetector.Detect(b));

        BinaryPrimitives.WriteUInt64BigEndian(b.AsSpan(8), ulong.MaxValue);
        Assert.Equal("avif", HeaderDetector.Detect(b));

        BinaryPrimitives.WriteUInt64BigEndian(b.AsSpan(8), 20); // too small for a largesize box: 24 at least
        Assert.Null(HeaderDetector.Detect(b));
    }

    [Fact]
    public void Truncated_ftyp_is_unknown()
    {
        Assert.Null(HeaderDetector.Detect(Ftyp("avif").AsSpan(0, 11)));
        Assert.Null(HeaderDetector.Detect(Ftyp("avif").AsSpan(0, 8)));
    }

    // --- the 64 KB limit -----------------------------------------------------------------------------------------------

    [Fact]
    public void Only_the_first_64_KB_count()
    {
        var data = new byte[HeaderDetector.MaxHeaderBytes + 100];
        var ftyp = FtypWithSize(0, "mif1");
        // ftyp with a size of 0 would extend to the end of the data: an avif brand placed after 64 KB must not be seen.
        ftyp.CopyTo(data, 0);
        Ascii("avif").CopyTo(data, HeaderDetector.MaxHeaderBytes + 8);
        Assert.Null(HeaderDetector.Detect(data));

        // ...while the same brand inside the first 64 KB is.
        Ascii("avif").CopyTo(data, HeaderDetector.MaxHeaderBytes - 4);
        Assert.Equal("avif", HeaderDetector.Detect(data));
    }
}
