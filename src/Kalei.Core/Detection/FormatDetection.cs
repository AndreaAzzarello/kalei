namespace Kalei.Core.Detection;

/// <summary>
/// What the Core knows about a file after the header stage. <paramref name="Format"/> is null when the file cannot be determined:
/// the Category is then "other" and no conversion is offered. <paramref name="ExtensionMismatch"/> is the MISMATCH notice of
/// docs/ipc-spec.md ("Notices"): raised by the Core, never an error, and the content always wins.
/// </summary>
public sealed record DetectionResult(string? Format, string Category, bool ExtensionMismatch);

public static class FormatDetection
{
    public static DetectionResult Classify(ReadOnlySpan<byte> header, string? extension)
    {
        var format = HeaderDetector.Detect(header);
        if (format is null)
        {
            return new DetectionResult(null, "other", false);
        }

        return new DetectionResult(format, "image", !string.IsNullOrEmpty(extension) && !Matches(format, extension));
    }

    private static bool Matches(string format, string extension)
    {
        var ext = extension.AsSpan().TrimStart('.');
        var accepted = format switch
        {
            "png" => ["png"],
            "jpg" => ["jpg", "jpeg", "jpe", "jfif"],
            "gif" => ["gif"],
            "webp" => ["webp"],
            "bmp" => ["bmp", "dib"],
            "tiff" => ["tif", "tiff"],
            "ico" => ["ico"],
            "avif" => ["avif", "avifs"],
            "heic" => ["heic", "heif"],
            _ => Array.Empty<string>(),
        };
        foreach (var a in accepted)
        {
            if (ext.Equals(a, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
