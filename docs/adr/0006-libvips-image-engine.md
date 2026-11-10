---
status: proposed
---
# libvips (via NetVips) as the image engine

The image Module uses NetVips (MIT) over libvips (LGPL, dynamically linked): streaming with low memory, good PNG/JPG/WebP/GIF/TIFF/AVIF coverage, and fuzzed upstream. Untrusted loaders are blocked (`vips_block_untrusted_set` or equivalent) and only loaders for supported formats are enabled. ICO uses a dedicated encoder; HEIC goes through WIC.

Status is *proposed* until the feasibility spike confirms that the prebuilt NetVips.Native build really includes AVIF writing and GIF via cgif, and that it runs inside the sandbox.

## Result of the engine check (ticket 02, W6, 2026-11-10)

Run on NetVips 3.2.0 with NetVips.Native.win-x64 8.18.7, outside the sandbox (the sandbox part is W1, ticket 01, still open, so the status stays *proposed*):
- **Present**: loaders for JPEG, PNG, WebP, TIFF, GIF, HEIF/AVIF (AV1 only) and SVG; savers for JPEG, PNG, WebP, TIFF, GIF (`gifsave`) and HEIF with AV1 (AVIF). AVIF and GIF writing, which this ADR needed, are confirmed.
- **Absent**: ImageMagick (`magickload`/`magicksave`), JPEG 2000, JPEG XL, PDF, OpenSlide, FITS, MATLAB, NIfTI, OpenEXR, PPM, Radiance. No native BMP or ICO codec.
- **No HEVC**: libheif in the package has no HEVC decoder or encoder ("Support for this compression format has not been built in: HEVC"), so HEIC cannot be read by libvips. This confirms the decision above: HEIC goes through WIC.
- **Blocking**: `Operation.Block("VipsForeignLoadSvg", true)` makes SVG loading fail (checked by behaviour, because the call does nothing silently on an unknown name); with `BlockUntrusted = true` SVG is blocked too, and PNG and AVIF still load. The package contains **no** ImageMagick, OpenSlide, MATLAB, FITS, NIfTI, OpenEXR, PDF or JPEG 2000/XL loader, so the untrusted class has nothing to block except SVG; we still set both blocks at start-up.
- **Caution for tests**: `Image.NewFromBuffer` reads only the header; a decode failure shows up later, at the first pixel read.

## Considered options

- **ImageSharp**: Six Labors split licence is risky for forks.
- **Magick.NET**: heavier, longer CVE history.
- **SkiaSharp only**: no TIFF, no AVIF, no GIF writing.
