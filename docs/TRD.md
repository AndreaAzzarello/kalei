# Kalei — Technical Requirements

Shape and security rules: [architecture.md](architecture.md). Decisions: [adr/](adr/). Pairs: [conversion-matrix.md](conversion-matrix.md).

## Stack
.NET 10, Avalonia 12 (12.1.6 on NuGet, MIT). UI app published with Native AOT; Workers are self-contained, trimmed, not AOT ([ADR 0001](adr/0001-avalonia-net10-aot-ui-only.md)). Installer on Windows: Inno Setup, per-user (`PrivilegesRequired=lowest`); portable ZIP. Other systems and their packaging: see Platform support below and [ADR 0010](adr/0010-multiplatform-architecture-windows-first.md). Tests: xUnit via `dotnet test`. Build: `dotnet build`.

## Engines and licences
Data from primary sources (NuGet pages, official repos), checked 2026-11-10 by a research pass. **n.v.** = not verified; the check is a to-do in Ticket 0 or in the ticket that adds the library. Sizes are NuGet package sizes, not final installed size.

### 1.0 (built-in)

| Family | Library | Licence | Version | Size | Trimming (Worker) | Notes |
|---|---|---|---|---|---|---|
| App | Avalonia | MIT | 12.1.6 | 9.7 MB pkg | AOT documented by Avalonia (needs `IsAotCompatible`, trimmer roots) | |
| Images | NetVips | MIT | 3.2.0 | 0.26 MB | n.v. | wrapper |
| Images | NetVips.Native.win-x64 (libvips) | **LGPL-3.0-or-later** (libvips itself: LGPL-2.1+) | 8.18.7 | 7.7 MB | n.v. | dynamic link; ship licence + source offer. Declares JPEG, PNG, WebP, AVIF, TIFF, GIF, SVG(in). **Not declared: BMP and ICO.** The SVG loader is not in the 1.0 matrix, so it must be disabled (attack surface). **Not confirmed:** AVIF encode, GIF via cgif, HEIF/HEVC backend, `vips_block_untrusted_set`, which loaders are compiled in (all checked in Ticket 0, W6) |
| Images | BMP and ICO codecs | own code (MIT) | – | small | – | small decoders/encoders inside the Worker, with fuzz tests, unless W6 shows an engine that does them; if neither, the format leaves the 1.0 matrix |
| Images | WIC (Windows) | OS component | – | 0 | – | HEIC read only, best effort. Reported to need the HEIF **and** the HEVC extensions of the user's Windows (not verified; Ticket 0, W5) |
| Data | Sep | MIT | 0.17.1 | n.v. | README: trimmable + AOT | candidate CSV |
| Data | CsvHelper | MS-PL OR Apache-2.0 | 34.0.0 | 1.1 MB | `IsTrimmable` + `IsAotCompatible` (net8/net10) | candidate CSV; avoid `dynamic`/Linq |
| Data | System.Text.Json, System.Xml | MIT (BCL) | .NET 10 | 0 | trim-safe with source generators for JSON | DTD/external entities off |
| Data | YamlDotNet | MIT | 18.1.0 | 0.78 MB | n.v. | safe mode, no arbitrary types |
| Data | MiniExcel | Apache-2.0 | 1.46.0 | 0.98 MB | n.v. | streaming; repo is `mini-software/MiniExcel` |
| Text | Markdig | BSD-2-Clause | 1.6.0 | 1.3 MB | n.v. | |
| Text | ReverseMarkdown | MIT | 6.3.0 | 0.53 MB | declared AOT/trimming ready | |
| Text | HtmlSanitizer | MIT | 11.0.26 | 0.18 MB | n.v. | depends on AngleSharp |

CSV choice: **Sep** if it passes the trimming publish test (fast, MIT, AOT-friendly), else CsvHelper. Decided by the trimming test W9 in ticket 03a.
Every "n.v." trimming entry must pass a `dotnet publish` of the Worker with trimming and warnings-as-errors before it is adopted.

### Brand assets

| Asset | Licence | Notes |
|---|---|---|
| Bricolage Grotesque (logotype "kalei") | SIL OFL 1.1, "Copyright 2022 The Bricolage Grotesque Project Authors"; no Reserved Font Name declared | Read from upstream repo `ateliertriay/bricolage` (README + `OFL.txt`). Google Fonts page could not be read, so confirmed from upstream only. Upstream README says it started as a fork of Mayenne Sans (Jérémy Landes). **Mayenne Sans licence: not verified from a primary source** (see next row). Variable TTF ≈ 399 KB; the app embeds a **subset** and never loads it from the network. **Subset definition (the only one): the letters of "kalei", A–Z, 0–9, and the characters `. + -` used in Format names and Donut labels.** Ship `OFL.txt` in third-party notices |
| Mayenne Sans (ancestor of Bricolage Grotesque) | Secondhand only: Fonts In Use says "Made available under the SIL Open Font License (OFL) in 2018. As of 2022, the repository is no longer available." (https://fontsinuse.com/typefaces/124719/mayenne-sans) | OFL version, copyright line and Reserved Font Name **not found**; original repository and the archived Studio Triple page were not reachable (web.archive.org is blocked for the tool). Bricolage Grotesque's own `OFL.txt` governs what we ship; before the README or notices state anything about Mayenne Sans, get the original `OFL.txt` (ask the author or check the archived page by hand). Credit: https://www.atelierjulianlegendre.com/references/la-mayenne |

### Later modules (1.1 onwards; research kept short on purpose)

| Module | Engine | Licence | Status / finding |
|---|---|---|---|
| PDF render | PDFium via PDFiumCore 157.0.8100 (Apache-2.0, 456 KB wrapper) + native `bblanchon.PDFium.Win32` 159.0.8152 (10.8 MB nupkg; NuGet says Apache-2.0, the pdfium-binaries repo says MIT; PDFium itself is BSD-style + Apache-2.0 text) | see left | OK. Per-arch size not confirmed. Avoid PdfiumViewer (archived 2019). No runtime licence key found. Sources: nuget.org/packages/PDFiumCore, nuget.org/packages/bblanchon.PDFium.Win32, github.com/bblanchon/pdfium-binaries, pdfium.googlesource.com/pdfium/+/refs/heads/main/LICENSE |
| PDF create | **SkiaSharp `SKDocument.CreatePdf`** (SkiaSharp 4.155.0, MIT; Skia BSD-3-Clause) | MIT | Multiple pages (`BeginPage`), points as units, metadata (Title, Author, Subject, Keywords, Creator, Producer, Creation, Modified), PDF/A-2b flag. **Images→PDF: fine** (no fonts). **Text→PDF: fonts are always embedded and, in SkiaSharp's Windows build, NOT subset** (Skia only subsets with HarfBuzz; SkiaSharp's `native/windows/build.cake` sets `skia_use_harfbuzz=false` and does not set `skia_pdf_subset_harfbuzz`, so each used font is embedded whole; SkiaSharp issue #848 open, #2180 closed "not planned"). No automatic layout (no line wrapping, page flow) and no shaping unless `SkiaSharp.HarfBuzz` is added. ToUnicode map exists in Skia source, so text should be selectable, but **not tested on a rendered PDF**. Italian accents (precomposed Latin) expected fine, **untested**. SkiaSharp.NativeAssets.Win32 is an 83 MB nupkg (per-RID size not confirmed). Sources: github.com/mono/SkiaSharp, nuget.org/packages/SkiaSharp, learn.microsoft.com/en-us/dotnet/api/skiasharp.skdocument.createpdf, skia.googlesource.com/skia/+/refs/heads/main/src/pdf/SkPDFSubsetFont.cpp |
| PDF create (fallback) | PDFsharp | **≤ 6.2.x: MIT** (6.2.2 per NuGet); **6.5.0 (2026-10-13/14) and 7.0 preview: AGPL-3.0 dual** (AGPL or commercial; code must call `PdfSharpLicense.SetAgplLicense()`) | Latest stable unusable. Pinned 6.2.x is a fallback for text→PDF with layout, no upstream fixes after the switch; 6.2.3/6.2.4 NuGet licence fields not read by us (Filee pins 6.2.4 and comments "6.2 is MIT", `Directory.Packages.props:35-36`, secondary evidence). Sources: nuget.org/packages/PDFsharp (+ /6.2.2, /6.5.0), github.com/empira/PDFsharp `master/LICENSE`, docs.pdfsharp.net/General/History.html |
| PDF merge/optimise | qpdf 12.4.2 | Apache-2.0 (before v7: Artistic-2.0) | Command-line tool + C++ library, **not .NET**: run as a separate process in the Module. Linearize, encrypt, merge/split, create from supplied content; does not render or extract text. Windows binary size and object-stream compression **not confirmed**. Source: github.com/qpdf/qpdf |
| Audio/video | FFmpeg, BtbN "lgpl" build | LGPL (build flavour; licence text not stated on the page) | gyan.dev builds are **GPLv3 only**, unusable. Not confirmed in BtbN LGPL build: libvpx, SVT-AV1, libopus, libmp3lame, `h264_mf` |
| Documents | pandoc 3.13 | GPL-2.0-or-later | separate process; ~43 MB; ship licence + source offer |
| Archives | SharpCompress | MIT (SPDX n.v.) | version n.v. |
| Archives | 7-Zip 26.06 (7za) | LGPL-2.1+ with unRAR restriction | no RAR |
| Vector | resvg | Apache-2.0 OR MIT | no .NET binding found: separate binary |
| 3D | Assimp | BSD-3-Clause (modified; SPDX n.v.) | |
| Signing | SignPath Foundation | free for OSS (claimed) | eligibility terms **not read**: verify **before 1.0**, because 1.0 and 1.1 would otherwise ship unsigned (Ticket 0, W18) |
| Installer | Inno Setup | permissive licence text, but the site asks commercial users to buy a licence | verify the exact `LICENSE.TXT` of the version used before the installer ticket (Ticket 0, W18); alternative if unsuitable |
| CI | GitHub Actions | free for public repositories with standard GitHub-hosted runners (verified 2026-11-10 on docs.github.com/en/billing/concepts/product-billing/github-actions; larger runners are billed there and are not used) | the page does not mention Windows runners separately: check the first workflow run on the billing page; if anything is charged, stop and ask |

### PDF module — proposed plan (decided when the module is scheduled)
Images→PDF with SKDocument; render with PDFium; merge/optimise with qpdf (separate process). Text→PDF is the open point: either SKDocument with one small bundled text font (embedded whole in every file) and our own simple line wrapping, or the pinned PDFsharp 6.2.x fallback. Decide with a test PDF check: selectable text, Italian accents, file size. Not to be chosen before that test.

### Hook (global Donut, 1.2)
| Component | Licence | Finding |
|---|---|---|
| SharpHook 8.0.1 | MIT (LICENSE + NuGet) | Wrapper of libuiohook; NuGet lists no dependencies; README implies it ships `uiohook.dll` under `lib/win-<platform>` (package contents not inspected). |
| libuiohook (kwhat; the only place this analysis is written, [ADR 0009](adr/0009-mouse-only-hook-and-sentinel.md) points here) | GPL-3.0 text (`COPYING.md`) **and** LGPL-3.0 text (`COPYING.LESSER.md`) in the repo, branch `1.2`; **no statement found about which applies and no GPL linking exception found** | Default build is a shared library. SharpHook builds from a fork (`TolikPylypchuk/libuiohook`, not read). Filee's notices describe it as "GPL-3.0 with linking exception / LGPL-3.0" (secondary, unconfirmed by the sources read). Decision: **write our own minimal `WH_MOUSE_LL` hook** (ADR 0009); no dependency, no licence question. |

## Platform support
### Sandbox mechanisms per OS
Chosen mechanisms, sources and limits: [ADR 0010](adr/0010-multiplatform-architecture-windows-first.md); per-OS table of every OS-dependent element: [architecture.md](architecture.md#capability-table-per-os). New libraries are not needed for them: Landlock, seccomp, App Sandbox and AppContainer are called through OS APIs from the platform adapters (P/Invoke or small native shims, to be sized in the spikes).

### Future distribution
- **Linux** (decided in the Linux spike): **Flatpak/Flathub** gives a built-in sandbox, with "no access to the network" by default, but needs the Flatpak runtime ([docs](https://docs.flatpak.org/en/latest/sandbox-permissions.html)). **AppImage** needs no installation but needs FUSE; Ubuntu 24.04 needs the `libfuse2t64` package, and `--appimage-extract-and-run` is "a last resort" ([AppImage docs](https://docs.appimage.org/user-guide/troubleshooting/fuse.html)); a bwrap inside it probably fails on Ubuntu 24.04 (see ADR 0010). **.deb** can carry its own AppArmor profile that allows user namespaces and installs to `/usr`, but needs root and covers only Debian and Ubuntu (not verified against a primary source). Proposal: Flatpak as the main format, AppImage as the secondary one (with the base sandbox only).
- **macOS**: `.dmg`, Developer ID signed and notarised when an account exists (`notarytool`, `stapler`); otherwise an ad-hoc signed `.dmg` with the "Open Anyway" instructions (plan B, ADR 0010). The sandbox does not depend on either.

## Open technical checks
All open proofs, with measures, pass/fail thresholds and fallbacks, are in [ticket-0.md](ticket-0.md) (Windows blocking 1.0; Linux and macOS spikes not blocking). Checks that belong to a later ticket, not repeated there:
- Trimming test for each library marked n.v. (W9 covers the 1.0 ones).
- Original `OFL.txt` of Mayenne Sans (ask the author or use the archived Studio Triple page by hand).
- PDF: a rendered test PDF from `SKDocument` (selectable text, Italian accents, size with and without a bundled font), when the PDF module is scheduled.
- Redistribution notices: THIRD-PARTY-NOTICES generated at build, shown in About.
