using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using NetVips;
using Lib = NetVips.NetVips;
using Xunit;
using Xunit.Abstractions;

namespace ImageEngineSpike.Tests;

/// <summary>
/// W5 (outside the sandbox): can HEIC be decoded, by libvips and by WIC, and which WIC decoders are registered?
/// Which Windows extensions are REQUIRED cannot be proved here without uninstalling them: see ticket 18.
/// </summary>
public class HeicTests(ITestOutputHelper output)
{
    static readonly Guid ContainerFormatHeif = new("e1e62521-6787-405b-a339-500715b5763f");

    static Image Sample() => Image.Black(64, 64).NewFromImage(10, 120, 200).Copy(interpretation: Enums.Interpretation.Srgb);

    [Fact]
    public void Libvips_heif_backend_cannot_write_hevc()
    {
        using var src = Sample();
        var ex = Record.Exception(() => src.HeifsaveBuffer(compression: Enums.ForeignHeifCompression.Hevc));
        output.WriteLine("heifsave hevc: " + (ex is null ? "WORKS" : "fails: " + ex.Message.Trim()));
        // Expected: the prebuilt libheif has no HEVC codec (AV1 only). Informational for ticket 18.
        Assert.NotNull(ex);
    }

    [Fact]
    public void Wic_registered_decoders_are_listed()
    {
        const string category = @"SOFTWARE\Classes\CLSID\{7ED96837-96F0-4812-B211-F13C24117ED3}\Instance";
        using var cat = Registry.LocalMachine.OpenSubKey(category);
        Assert.NotNull(cat);
        foreach (var clsid in cat!.GetSubKeyNames())
        {
            using var k = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Classes\CLSID\{clsid}");
            output.WriteLine($"{clsid} | {k?.GetValue("FriendlyName")} | {k?.GetValue("MimeTypes")} | {k?.GetValue("FileExtensions")}");
        }
        // Packaged extensions (HEIF, HEVC, AV1, WebP, RAW from the Store) may register their decoders as packaged COM
        // and not appear in this list: absence here proves nothing, the decode test below is the proof.
    }

    [Fact]
    public void Wic_decodes_heic_and_libvips_does_or_does_not()
    {
        byte[] heic;
        try
        {
            var enc = BitmapEncoder.Create(ContainerFormatHeif);
            var px = new byte[64 * 64 * 4];
            for (var i = 0; i < px.Length; i += 4) { px[i] = 200; px[i + 1] = 120; px[i + 2] = 10; px[i + 3] = 255; }
            enc.Frames.Add(BitmapFrame.Create(BitmapSource.Create(64, 64, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, px, 64 * 4)));
            using var ms = new MemoryStream();
            enc.Save(ms);
            heic = ms.ToArray();
        }
        catch (Exception e)
        {
            output.WriteLine("WIC HEIF ENCODE not available here: " + e.GetType().Name + ": " + e.Message.Trim());
            output.WriteLine("=> no HEIC sample could be generated locally; decode NOT verified (needs a sample from the owner, ticket 18).");
            return;
        }
        output.WriteLine($"WIC wrote a HEIC sample, {heic.Length} bytes, ftyp={System.Text.Encoding.ASCII.GetString(heic, 8, 4)}");

        using (var ms = new MemoryStream(heic))
        {
            var dec = BitmapDecoder.Create(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            output.WriteLine($"WIC DECODE OK: {dec.Frames[0].PixelWidth}x{dec.Frames[0].PixelHeight}, decoder={dec.CodecInfo.FriendlyName}");
            Assert.Equal(64, dec.Frames[0].PixelWidth);
        }

        var ex = Record.Exception(() =>
        {
            using var v = Image.NewFromBuffer(heic);
            var avg = v.Flatten(background: [0.0]).Avg();
            output.WriteLine($"libvips decoded {v.Width}x{v.Height}, bands={v.Bands}, loader={v.Get("vips-loader")}, mean value={avg:F1} (sample is R200 G120 B10, mean 110)");
        });
        // NewFromBuffer is lazy (header only); Avg() above forces the pixel decode, which is what fails.
        output.WriteLine("libvips decode of the same HEIC: " + (ex is null ? "WORKS" : "fails at pixel decode (libheif has no HEVC decoder): " + ex.Message.Trim()));
        Assert.NotNull(ex); // result: HEIC must go through WIC (ticket 18)
    }

    [Fact]
    public void Block_untrusted_effect_on_svg_and_heif_is_recorded()
    {
        const string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='8' height='8'><rect width='8' height='8'/></svg>";
        var avif = Sample().HeifsaveBuffer(compression: Enums.ForeignHeifCompression.Av1);
        Lib.BlockUntrusted = true;
        try
        {
            var svgEx = Record.Exception(() => { using var _ = Image.NewFromBuffer(System.Text.Encoding.UTF8.GetBytes(svg)); });
            var heifEx = Record.Exception(() => { using var _ = Image.NewFromBuffer(avif); });
            output.WriteLine("with BlockUntrusted=true: svg " + (svgEx is null ? "still loads" : "blocked") + ", heif/avif " + (heifEx is null ? "still loads" : "blocked"));
        }
        finally { Lib.BlockUntrusted = false; }
    }
}
