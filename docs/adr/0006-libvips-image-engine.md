---
status: accepted (Windows 11 and Windows 10 22H2)
---
# libvips (via NetVips) as the image engine

The image Module uses NetVips (MIT) over libvips (LGPL, dynamically linked): streaming with low memory, good PNG/JPG/WebP/GIF/TIFF/AVIF coverage, and fuzzed upstream. Untrusted loaders are blocked (`vips_block_untrusted_set` or equivalent) and only loaders for supported formats are enabled. ICO uses a dedicated encoder; HEIC goes through WIC.

The feasibility spike confirmed that the prebuilt NetVips.Native build includes AVIF writing and GIF via cgif (ticket 02) and that it runs inside the sandbox (ticket 01, below). Trimming of NetVips (W9) is checked in ticket 03a.

## Result of the engine check (ticket 02, W6, 2026-11-10)

Run on NetVips 3.2.0 with NetVips.Native.win-x64 8.18.7, outside the sandbox (inside the sandbox: see "Result inside the sandbox" at the end of this section):
- **Present**: loaders for JPEG, PNG, WebP, TIFF, GIF, HEIF/AVIF (AV1 only) and SVG; savers for JPEG, PNG, WebP, TIFF, GIF (`gifsave`) and HEIF with AV1 (AVIF). AVIF and GIF writing, which this ADR needed, are confirmed.
- **Absent**: ImageMagick (`magickload`/`magicksave`), JPEG 2000, JPEG XL, PDF, OpenSlide, FITS, MATLAB, NIfTI, OpenEXR, PPM, Radiance. No native BMP or ICO codec.
- **No HEVC**: libheif in the package has no HEVC decoder or encoder ("Support for this compression format has not been built in: HEVC"), so HEIC cannot be read by libvips. This confirms the decision above: HEIC goes through WIC.
- **Blocking**: `Operation.Block("VipsForeignLoadSvg", true)` makes SVG loading fail (checked by behaviour, because the call does nothing silently on an unknown name); with `BlockUntrusted = true` SVG is blocked too, and PNG and AVIF still load. The package contains **no** ImageMagick, OpenSlide, MATLAB, FITS, NIfTI, OpenEXR, PDF or JPEG 2000/XL loader, so the untrusted class has nothing to block except SVG; we still set both blocks at start-up.
- **Caution for tests**: `Image.NewFromBuffer` reads only the header; a decode failure shows up later, at the first pixel read.
- **Result inside the sandbox** (ticket 01, 2026-11-10, Windows 11 PC, see [ADR 0003](0003-worker-sandbox-and-handle-passing.md)): NetVips and libvips 8.18.7 load in an AppContainer without capabilities from a per-user folder and convert PNG to AVIF and to WebP reading and writing through inherited handles (output decoded and checked in pixels); a tiled pyramid TIFF and an AVIF are read through seekable handles.
- **Result inside the sandbox on the reference VM** (issue #47, 2026-11-11, Windows 10 22H2 build 19045.6456): the same package gives the same result: AVIF (4,202 bytes) and WebP (9,780 bytes) written and checked in pixels, the pyramid TIFF and the AVIF read through seekable handles (887 and 226 seeks). Details in [ADR 0003](0003-worker-sandbox-and-handle-passing.md).

## Considered options

- **ImageSharp**: Six Labors split licence is risky for forks.
- **Magick.NET**: heavier, longer CVE history.
- **SkiaSharp only**: no TIFF, no AVIF, no GIF writing.
