using Kalei.Core.Detection;
using Xunit;

namespace Kalei.Tests.Detection;

public sealed class FormatDetectionTests
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0];

    [Theory]
    [InlineData(".png")]
    [InlineData("PNG")]
    [InlineData(null)]
    [InlineData("")]
    public void Matching_or_missing_extension_is_no_mismatch(string? ext) =>
        Assert.Equal(new DetectionResult("png", "image", false), FormatDetection.Classify(Png, ext));

    [Theory]
    [InlineData(".jpg")]
    [InlineData("txt")]
    [InlineData(".jpeg")]
    public void Wrong_extension_is_a_mismatch_and_the_content_wins(string ext) =>
        Assert.Equal(new DetectionResult("png", "image", true), FormatDetection.Classify(Png, ext));

    [Theory]
    [InlineData(".jpeg")]
    [InlineData(".JFIF")]
    public void Jpg_accepts_its_usual_extensions(string ext) =>
        Assert.False(FormatDetection.Classify([0xFF, 0xD8, 0xFF, 0xE0], ext).ExtensionMismatch);

    [Theory]
    [InlineData(".png")]
    [InlineData(null)]
    public void Unknown_content_is_other_without_mismatch(string? ext) =>
        Assert.Equal(new DetectionResult(null, "other", false), FormatDetection.Classify("not an image"u8, ext));

    [Fact]
    public void Empty_file_is_other() =>
        Assert.Equal(new DetectionResult(null, "other", false), FormatDetection.Classify([], ".png"));
}
