using System.Buffers.Binary;

namespace Kalei.Core.Detection;

/// <summary>
/// Header stage of docs/format-detection.md for images (S3): matches signatures in the first bytes and nothing else.
/// Pure and allocation-free; it never throws, whatever the bytes are. Result: the Format id used by the IPC and the
/// manifests ("png", "jpg", "gif", "webp", "bmp", "tiff", "ico", "avif", "heic"), or null when it cannot be determined.
/// </summary>
public static class HeaderDetector
{
    /// <summary>S3: the Core never looks beyond this many bytes of a file.</summary>
    public const int MaxHeaderBytes = 64 * 1024;

    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static ReadOnlySpan<byte> JpgSignature => [0xFF, 0xD8, 0xFF];

    private static ReadOnlySpan<byte> TiffLittleEndian => [0x49, 0x49, 0x2A, 0x00];

    private static ReadOnlySpan<byte> TiffBigEndian => [0x4D, 0x4D, 0x00, 0x2A];

    private static ReadOnlySpan<byte> IcoSignature => [0x00, 0x00, 0x01, 0x00];

    // Four-character codes, big endian.
    private const uint Ftyp = 0x66747970;
    private const uint Avif = 0x61766966;
    private const uint Avis = 0x61766973;
    private const uint Heic = 0x68656963;
    private const uint Heix = 0x68656978;
    private const uint Hevc = 0x68657663;
    private const uint Hevx = 0x68657678;
    private const uint Mif1 = 0x6D696631;
    private const uint Msf1 = 0x6D736631;

    public static string? Detect(ReadOnlySpan<byte> header)
    {
        if (header.Length > MaxHeaderBytes)
        {
            header = header[..MaxHeaderBytes];
        }

        if (header.StartsWith(PngSignature))
        {
            return "png";
        }

        if (header.StartsWith(JpgSignature))
        {
            return "jpg";
        }

        if (header.StartsWith("GIF87a"u8) || header.StartsWith("GIF89a"u8))
        {
            return "gif";
        }

        if (header.Length >= 12 && header.StartsWith("RIFF"u8) && header[8..].StartsWith("WEBP"u8))
        {
            return "webp";
        }

        if (header.StartsWith(TiffLittleEndian) || header.StartsWith(TiffBigEndian))
        {
            return "tiff";
        }

        if (header.Length >= 18 && header.StartsWith("BM"u8)
            && BinaryPrimitives.ReadUInt32LittleEndian(header[14..]) is 12 or 40 or 52 or 56 or 108 or 124)
        {
            return "bmp";
        }

        // 00 00 02 00 is CUR: not supported.
        if (header.Length >= 6 && header.StartsWith(IcoSignature)
            && BinaryPrimitives.ReadUInt16LittleEndian(header[4..]) is >= 1 and <= 255)
        {
            return "ico";
        }

        if (header.Length >= 12 && BinaryPrimitives.ReadUInt32BigEndian(header[4..]) == Ftyp)
        {
            return DetectBrand(header);
        }

        return null;
    }

    /// <summary>
    /// ftyp rule (docs/format-detection.md): the major brand decides when it is avif/avis (AVIF) or heic/heix/hevc/hevx (HEIC);
    /// only for mif1/msf1 are the compatible brands read, and if avif and heic are both there, AVIF wins. Any other major is unknown.
    /// </summary>
    private static string? DetectBrand(ReadOnlySpan<byte> header)
    {
        long size = BinaryPrimitives.ReadUInt32BigEndian(header);
        var brandsAt = 8;
        if (size == 1)
        {
            // 64-bit size after the type; a header too short to hold it cannot be a valid box.
            if (header.Length < 16)
            {
                return null;
            }

            var big = BinaryPrimitives.ReadUInt64BigEndian(header[8..]);
            size = big > int.MaxValue ? int.MaxValue : (long)big;
            brandsAt = 16;
        }
        else if (size == 0)
        {
            size = header.Length;
        }

        // The box holds at least the major brand and the minor version.
        if (size < brandsAt + 8 || header.Length < brandsAt + 4)
        {
            return null;
        }

        var end = (int)Math.Min(size, header.Length);
        switch (BinaryPrimitives.ReadUInt32BigEndian(header[brandsAt..]))
        {
            case Avif or Avis:
                return "avif";
            case Heic or Heix or Hevc or Hevx:
                return "heic";
            case Mif1 or Msf1:
                var sawHeic = false;
                for (var p = brandsAt + 8; p + 4 <= end; p += 4)
                {
                    switch (BinaryPrimitives.ReadUInt32BigEndian(header[p..]))
                    {
                        case Avif:
                            return "avif";
                        case Heic:
                            sawHeic = true;
                            break;
                    }
                }

                return sawHeic ? "heic" : null;
            default:
                return null;
        }
    }
}
