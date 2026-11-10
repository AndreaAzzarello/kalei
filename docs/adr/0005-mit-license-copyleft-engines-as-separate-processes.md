---
status: accepted
---
# MIT for Kalei; copyleft engines run as separate processes

Kalei's own code is MIT. Engines under GPL (e.g. pandoc) are never linked: they run as separate processes inside their Module, shipped with their own license text and a link or written offer for the exact source version. LGPL engines (libvips, FFmpeg LGPL build, LAME) are dynamically linked or run as separate binaries. FFmpeg is the LGPL build without x264/x265; H.264 uses Media Foundation (`h264_mf`) or hardware encoders. No HEVC codec is ever distributed (patents); HEIC works only through the user's own WIC HEVC extension, best effort.

Libraries with AGPL or commercial licences (QuestPDF, iText, MuPDF, Ghostscript, ImageSharp split licence, EPPlus, PDFsharp from 6.5.0 on) are excluded so Kalei can stay free and open source, forks included. PDFsharp up to 6.2.x is MIT and is allowed only as a version-pinned fallback documented in the [TRD](../TRD.md) (no upstream fixes after the licence change). LibreOffice is not bundled; if already installed on the PC it may be used as an optional external engine (to be verified inside the sandbox).

## Considered options

- GPL-3.0 for everything: simpler legally, but less permissive for reuse of the core.
