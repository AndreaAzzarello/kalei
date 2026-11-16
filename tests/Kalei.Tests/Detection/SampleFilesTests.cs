using Kalei.Core.Detection;
using Xunit;

namespace Kalei.Tests.Detection;

/// <summary>Ticket 06 criterion 5: for every Format a valid, a truncated and a wrong-extension sample (tests/samples/README.md).</summary>
public sealed class SampleFilesTests
{
    private static readonly string[] Formats = ["png", "jpg", "gif", "webp", "tiff", "bmp", "ico", "avif", "heic"];

    public static TheoryData<string> AllFormats() => [.. Formats];

    private static string SamplesDir(string format)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Kalei.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return Path.Combine(dir.FullName, "tests", "samples", format);
    }

    private static DetectionResult Classify(string path)
    {
        var buffer = new byte[HeaderDetector.MaxHeaderBytes];
        using var stream = File.OpenRead(path);
        var n = HeaderReader.Read(stream, buffer);
        return FormatDetection.Classify(buffer.AsSpan(0, n), Path.GetExtension(path));
    }

    [Theory]
    [MemberData(nameof(AllFormats))]
    public void Valid_sample_gives_its_format(string format)
    {
        var file = Directory.GetFiles(SamplesDir(format), "valid*").Single();
        Assert.Equal(new DetectionResult(format, "image", false), Classify(file));
    }

    [Theory]
    [MemberData(nameof(AllFormats))]
    public void Truncated_sample_is_other(string format)
    {
        var file = Directory.GetFiles(SamplesDir(format), "truncated*").Single();
        Assert.Equal(new DetectionResult(null, "other", false), Classify(file));
    }

    [Theory]
    [MemberData(nameof(AllFormats))]
    public void Wrong_extension_is_recognised_from_the_content(string format)
    {
        var file = Directory.GetFiles(SamplesDir(format), $"{format}-named-*").Single();
        Assert.Equal(new DetectionResult(format, "image", true), Classify(file));
    }
}
