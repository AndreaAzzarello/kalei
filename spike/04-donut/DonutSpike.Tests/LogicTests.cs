using DonutSpike.Logic;
using Xunit;

namespace DonutSpike.Tests;

public class FormatHintTests
{
    [Theory]
    [InlineData("photo.PNG", "png", "image")]
    [InlineData("a.jpeg", "jpg", "image")]
    [InlineData("table.xlsx", "xlsx", "spreadsheet")]
    [InlineData("data.csv", "csv", "spreadsheet")]
    [InlineData("config.yml", "yaml", "document")]
    [InlineData("notes.md", "md", "document")]
    public void Extension_gives_format_and_category(string name, string format, string category)
    {
        var h = FormatHint.FromName(name);
        Assert.True(h.Known);
        Assert.Equal(format, h.Format);
        Assert.Equal(category, h.Category);
    }

    [Theory]
    [InlineData("noext")]
    [InlineData("archive.xyz")]
    [InlineData("")]
    public void Unknown_extension_is_not_known(string name)
    {
        var h = FormatHint.FromName(name);
        Assert.False(h.Known);
        Assert.Equal("unknown", h.Category);
    }

    [Fact]
    public void Header_wins_over_a_wrong_extension()
    {
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];
        var h = FormatHint.FromHeader(png, "photo.jpg");
        Assert.Equal("png", h.Format);
        Assert.True(h.ExtensionMismatch);
    }

    [Fact]
    public void Header_matching_extension_is_not_a_mismatch()
    {
        byte[] jpg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0];
        var h = FormatHint.FromHeader(jpg, "a.jpeg");
        Assert.Equal("jpg", h.Format);
        Assert.False(h.ExtensionMismatch);
    }

    [Fact]
    public void Unrecognised_header_falls_back_to_extension()
    {
        var h = FormatHint.FromHeader(new byte[12], "a.csv");
        Assert.Equal("csv", h.Format);
        Assert.False(h.ExtensionMismatch);
    }

    [Fact]
    public void Empty_or_short_header_does_not_throw()
    {
        Assert.False(FormatHint.FromHeader(ReadOnlySpan<byte>.Empty, "x").Known);
        Assert.Equal("gif", FormatHint.FromHeader("GIF89a"u8, "x.gif").Format);
    }
}

public class DonutGeometryTests
{
    [Fact]
    public void Ring_in_the_middle_is_centred_on_the_cursor()
    {
        var r = DonutGeometry.Place(500, 400, 260, 1000, 800, 8);
        Assert.Equal(370, r.Left);
        Assert.Equal(270, r.Top);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1000, 800)]
    [InlineData(5, 790)]
    [InlineData(-50, 900)]
    public void Ring_is_never_clipped_by_the_window(double x, double y)
    {
        var r = DonutGeometry.Place(x, y, 260, 1000, 800, 8);
        Assert.True(r.Left >= 8 && r.Top >= 8);
        Assert.True(r.Left + 260 <= 1000 - 8);
        Assert.True(r.Top + 260 <= 800 - 8);
    }

    [Fact]
    public void Window_smaller_than_ring_is_centred_not_negative()
    {
        var r = DonutGeometry.Place(10, 10, 260, 200, 200, 8);
        Assert.Equal(-30, r.Left);
        Assert.Equal(-30, r.Top);
    }

    [Fact]
    public void Eight_slices_are_evenly_spaced_on_the_radius()
    {
        var pts = DonutGeometry.SliceCentres(8, 100);
        Assert.Equal(8, pts.Count);
        foreach (var (x, y) in pts)
            Assert.Equal(100, Math.Sqrt(x * x + y * y), 6);
        Assert.Equal(0, pts[0].X, 6);
        Assert.Equal(-100, pts[0].Y, 6);
        Assert.Equal(100, pts[2].X, 6);
    }

    [Fact]
    public void Zero_slices_gives_an_empty_list() => Assert.Empty(DonutGeometry.SliceCentres(0, 100));
}

public class LatencyStatsTests
{
    [Fact]
    public void Median_of_odd_and_even_counts()
    {
        Assert.Equal(3, LatencyStats.Median([5, 1, 3]));
        Assert.Equal(2.5, LatencyStats.Median([4, 1, 2, 3]));
    }

    [Fact]
    public void Median_of_nothing_is_nan() => Assert.True(double.IsNaN(LatencyStats.Median([])));

    [Fact]
    public void Max_and_pass_rate_against_a_limit()
    {
        double[] v = [40, 60, 90, 120];
        Assert.Equal(120, LatencyStats.Max(v));
        Assert.Equal(0.75, LatencyStats.PassRate(v, 100));
    }

    [Fact]
    public void Ticks_convert_to_milliseconds()
    {
        Assert.Equal(1000, LatencyStats.ToMs(System.Diagnostics.Stopwatch.Frequency), 3);
    }
}
