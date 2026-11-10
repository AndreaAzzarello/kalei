namespace DonutSpike.Logic;

/// <summary>What is known about a dragged file before the drop.</summary>
public readonly record struct Hint(string Format, string Category, bool Known, bool ExtensionMismatch);

public static class FormatHint
{
    // Extension -> (format, category). Mirrors docs/conversion-matrix.md for 1.0 only as far as the spike needs.
    private static readonly Dictionary<string, (string Format, string Category)> ByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        ["png"] = ("png", "image"), ["jpg"] = ("jpg", "image"), ["jpeg"] = ("jpg", "image"), ["webp"] = ("webp", "image"),
        ["gif"] = ("gif", "image"), ["tif"] = ("tiff", "image"), ["tiff"] = ("tiff", "image"), ["avif"] = ("avif", "image"),
        ["bmp"] = ("bmp", "image"), ["ico"] = ("ico", "image"),
        ["csv"] = ("csv", "spreadsheet"), ["tsv"] = ("tsv", "spreadsheet"), ["xlsx"] = ("xlsx", "spreadsheet"),
        ["json"] = ("json", "document"), ["yaml"] = ("yaml", "document"), ["yml"] = ("yaml", "document"),
        ["xml"] = ("xml", "document"), ["txt"] = ("txt", "document"), ["md"] = ("md", "document"),
        ["html"] = ("html", "document"), ["htm"] = ("html", "document"),
    };

    private static readonly Hint Unknown = new("unknown", "unknown", false, false);

    public static Hint FromName(string fileName)
    {
        var ext = Path.GetExtension(fileName).TrimStart('.');
        return ext.Length > 0 && ByExtension.TryGetValue(ext, out var f)
            ? new Hint(f.Format, f.Category, true, false)
            : Unknown;
    }

    /// <summary>Format from the first bytes (at most 16 are needed); falls back to the extension.</summary>
    public static Hint FromHeader(ReadOnlySpan<byte> header, string fileName)
    {
        var byName = FromName(fileName);
        var sniffed = Sniff(header);
        if (sniffed is null) return byName;
        var category = ByExtension.Values.First(v => v.Format == sniffed).Category;
        return new Hint(sniffed, category, true, byName.Known && byName.Format != sniffed);
    }

    private static string? Sniff(ReadOnlySpan<byte> h)
    {
        if (h.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return "png";
        if (h.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF })) return "jpg";
        if (h.StartsWith("GIF8"u8)) return "gif";
        if (h.Length >= 12 && h[..4].SequenceEqual("RIFF"u8) && h.Slice(8, 4).SequenceEqual("WEBP"u8)) return "webp";
        if (h.StartsWith("BM"u8) && h.Length >= 14) return "bmp";
        if (h.StartsWith(new byte[] { 0x49, 0x49, 0x2A, 0x00 }) || h.StartsWith(new byte[] { 0x4D, 0x4D, 0x00, 0x2A })) return "tiff";
        return null;
    }
}

public static class DonutGeometry
{
    /// <summary>Top-left of a ring of the given diameter centred on the cursor, kept inside the window with a margin.
    /// If the window is smaller than the ring plus margins, the ring is centred (it will overflow equally).</summary>
    public static (double Left, double Top) Place(double cursorX, double cursorY, double diameter, double width, double height, double margin)
        => (Axis(cursorX, diameter, width, margin), Axis(cursorY, diameter, height, margin));

    private static double Axis(double cursor, double d, double size, double margin)
    {
        if (size < d + 2 * margin) return (size - d) / 2;
        return Math.Clamp(cursor - d / 2, margin, size - margin - d);
    }

    /// <summary>Centres of n slices on a circle of the given radius, the first at the top, clockwise.</summary>
    public static IReadOnlyList<(double X, double Y)> SliceCentres(int count, double radius)
    {
        var list = new List<(double, double)>(Math.Max(count, 0));
        for (var i = 0; i < count; i++)
        {
            var a = 2 * Math.PI * i / count;
            list.Add((radius * Math.Sin(a), -radius * Math.Cos(a)));
        }
        return list;
    }
}

public static class LatencyStats
{
    public static double ToMs(long ticks) => ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;

    public static double Median(IEnumerable<double> values)
    {
        var s = values.OrderBy(v => v).ToArray();
        if (s.Length == 0) return double.NaN;
        return s.Length % 2 == 1 ? s[s.Length / 2] : (s[s.Length / 2 - 1] + s[s.Length / 2]) / 2;
    }

    public static double Max(IEnumerable<double> values) => values.DefaultIfEmpty(double.NaN).Max();

    public static double PassRate(IEnumerable<double> values, double limit)
    {
        var a = values.ToArray();
        return a.Length == 0 ? double.NaN : a.Count(v => v <= limit) / (double)a.Length;
    }
}

/// <summary>Targets the in-app Donut would offer (docs/design-brief.md, "Donut faces"): up to 8, most common first.</summary>
public static class Targets
{
    private static readonly string[] Images = ["webp", "png", "jpg", "avif", "gif", "tiff", "bmp", "ico"];
    private static readonly string[] Sheets = ["csv", "xlsx", "json", "tsv"];
    private static readonly string[] DataDocs = ["json", "yaml", "xml", "csv", "tsv", "xlsx"];
    private static readonly string[] TextDocs = ["txt", "md", "html"];

    private static string[] Candidates(Hint h) => h.Category switch
    {
        "image" => Images,
        "spreadsheet" => Sheets,
        "document" when h.Format is "json" or "yaml" or "xml" => DataDocs,
        "document" => TextDocs,
        _ => [],
    };

    public static IEnumerable<string> For(IEnumerable<Hint> hints)
    {
        var list = hints.ToList();
        if (list.Count == 0 || list.Any(h => !h.Known)) return [];
        IEnumerable<string> common = Candidates(list[0]);
        foreach (var h in list.Skip(1)) common = common.Intersect(Candidates(h));
        var sources = list.Select(h => h.Format).Distinct().ToList();
        // A single source Format is excluded; with several Formats each stays (every file either converts to it or already is it).
        if (sources.Count == 1) common = common.Where(f => f != sources[0]);
        return common.Take(8).ToList();
    }
}
