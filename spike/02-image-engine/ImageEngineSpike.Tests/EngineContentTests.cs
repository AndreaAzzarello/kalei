using NetVips;
using Lib = NetVips.NetVips;
using Xunit;
using Xunit.Abstractions;

namespace ImageEngineSpike.Tests;

/// <summary>
/// W6: what the prebuilt libvips (NetVips.Native.win-x64) really contains.
/// A missing feature is a RESULT, not a bug: those tests assert the absence and say which ticket takes over.
/// </summary>
public class EngineContentTests(ITestOutputHelper output)
{
    static readonly string[] LoaderNicknames =
    [
        "jpegload", "pngload", "webpload", "tiffload", "gifload", "nsgifload", "heifload", "svgload", "jp2kload", "jxlload",
        "pdfload", "magickload", "openslideload", "ppmload", "radload", "vipsload", "fitsload", "matload", "niftiload",
        "analyzeload", "openexrload", "csvload", "matrixload", "rawload",
    ];

    static readonly string[] SaverNicknames =
    [
        "jpegsave", "pngsave", "webpsave", "tiffsave", "gifsave", "heifsave", "jp2ksave", "jxlsave", "magicksave",
        "ppmsave", "radsave", "vipssave", "fitssave", "matrixsave", "csvsave", "rawsave", "dzsave", "pdfsave",
    ];

    // NetVips does not expose type lookup, so call the operation without arguments:
    // an unknown nickname says so, a known one complains about its missing arguments.
    static bool Has(string nickname)
    {
        try { Operation.Call(nickname); return true; }
        catch (ArgumentException) { return true; } // known operation, missing arguments
        catch (VipsException e)
        {
            return !e.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase)
                && !e.Message.Contains("no such", StringComparison.OrdinalIgnoreCase);
        }
    }

    static Image Sample() =>
        Image.Black(64, 64).NewFromImage(10, 120, 200).Bandjoin(Image.Black(64, 64) + 128).Copy(interpretation: Enums.Interpretation.Srgb);

    [Fact]
    public void Inventory_lists_loaders_savers_and_version()
    {
        output.WriteLine($"libvips {Lib.Version(0)}.{Lib.Version(1)}.{Lib.Version(2)}");
        output.WriteLine("LOADERS: " + string.Join(", ", LoaderNicknames.Select(n => $"{n}={(Has(n) ? "yes" : "NO")}")));
        output.WriteLine("SAVERS: " + string.Join(", ", SaverNicknames.Select(n => $"{n}={(Has(n) ? "yes" : "NO")}")));
        output.WriteLine("SUFFIXES: " + string.Join(" ", Lib.GetSuffixes() ?? []));
        Assert.True(Has("pngload") && Has("pngsave"));
    }

    [Theory]
    [InlineData(".png")]
    [InlineData(".jpg")]
    [InlineData(".webp")]
    [InlineData(".tif")]
    [InlineData(".gif")]
    [InlineData(".avif")]
    public void Roundtrip_via_buffer_writes_and_reads_back(string suffix)
    {
        using var src = Sample();
        byte[] bytes;
        try { bytes = src.WriteToBuffer(suffix); }
        catch (VipsException e)
        {
            output.WriteLine($"{suffix}: WRITE FAILED: {e.Message.Trim()}");
            Assert.Fail($"{suffix} cannot be written: {e.Message.Trim()}");
            return;
        }
        using var back = Image.NewFromBuffer(bytes);
        var mean = back.Avg(); // forces the pixel decode (NewFromBuffer alone reads only the header)
        output.WriteLine($"{suffix}: wrote {bytes.Length} bytes, read back {back.Width}x{back.Height}, bands={back.Bands}, loader={back.Get("vips-loader")}, mean={mean:F1}");
        Assert.Equal(64, back.Width);
        Assert.Equal(64, back.Height);
    }

    [Fact]
    public void Avif_is_written_with_the_av1_codec_by_heifsave()
    {
        using var src = Sample();
        var bytes = src.HeifsaveBuffer(compression: Enums.ForeignHeifCompression.Av1);
        // ISO BMFF: 'ftyp' at offset 4, major brand 'avif' (or 'avis') at offset 8.
        var brand = System.Text.Encoding.ASCII.GetString(bytes, 8, 4);
        output.WriteLine($"ftyp brand = {brand}, {bytes.Length} bytes");
        Assert.Contains(brand, new[] { "avif", "avis" });
    }

    [Fact]
    public void Gif_saver_is_present()
    {
        output.WriteLine($"gifsave present: {Has("gifsave")}");
        Assert.True(Has("gifsave"));
    }

    [Theory]
    [InlineData(".bmp")]
    [InlineData(".ico")]
    public void Bmp_and_ico_are_not_handled_natively(string suffix)
    {
        // Expected result: no native libvips codec. If this ever starts failing the engine gained one: re-read ticket 17.
        using var src = Sample();
        var ex = Record.Exception(() => src.WriteToBuffer(suffix));
        output.WriteLine($"{suffix} write: {(ex is null ? "WORKS" : "fails: " + ex.Message.Trim())}; magicksave present: {Has("magicksave")}; magickload present: {Has("magickload")}");
        Assert.NotNull(ex);
        Assert.False(Has("magickload"));
    }

    [Fact]
    public void Block_untrusted_keeps_trusted_loaders_working()
    {
        var png = Sample().PngsaveBuffer();
        Lib.BlockUntrusted = true;
        try
        {
            using var back = Image.NewFromBuffer(png);
            Assert.Equal(64, back.Width);
            var untrustedPresent = new[] { "magickload", "openslideload", "matload", "fitsload", "niftiload", "analyzeload", "openexrload", "pdfload", "jp2kload", "jxlload" }
                .Where(Has).ToArray();
            output.WriteLine("Untrusted-class loaders compiled in: " + (untrustedPresent.Length == 0 ? "none" : string.Join(", ", untrustedPresent)));
        }
        finally { Lib.BlockUntrusted = false; }
    }

    [Fact]
    public void Svg_loader_can_be_disabled()
    {
        const string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='8' height='8'><rect width='8' height='8' fill='red'/></svg>";
        var bytes = System.Text.Encoding.UTF8.GetBytes(svg);
        output.WriteLine($"svgload present: {Has("svgload")}");
        Assert.True(Has("svgload"), "svgload not compiled in: nothing to disable (record as 'absent, no action needed')");

        using (var before = Image.NewFromBuffer(bytes)) Assert.Equal(8, before.Width);

        Operation.Block("VipsForeignLoadSvg", true);
        try
        {
            var ex = Record.Exception(() => { using var _ = Image.NewFromBuffer(bytes); });
            output.WriteLine("after block: " + (ex is null ? "STILL LOADS" : "refused: " + ex.Message.Trim()));
            Assert.NotNull(ex);
        }
        finally { Operation.Block("VipsForeignLoadSvg", false); }
    }
}
