---
status: proposed
---
# libvips (via NetVips) as the image engine

The image Module uses NetVips (MIT) over libvips (LGPL, dynamically linked): streaming with low memory, good PNG/JPG/WebP/GIF/TIFF/AVIF coverage, and fuzzed upstream. Untrusted loaders are blocked (`vips_block_untrusted_set` or equivalent) and only loaders for supported formats are enabled. ICO uses a dedicated encoder; HEIC goes through WIC.

Status is *proposed* until the feasibility spike confirms that the prebuilt NetVips.Native build really includes AVIF writing and GIF via cgif, and that it runs inside the sandbox.

## Considered options

- **ImageSharp**: Six Labors split licence is risky for forks.
- **Magick.NET**: heavier, longer CVE history.
- **SkiaSharp only**: no TIFF, no AVIF, no GIF writing.
