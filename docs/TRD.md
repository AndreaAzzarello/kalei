# Kalei — Technical Requirements

Shape and security rules: [architecture.md](architecture.md). Decisions: [adr/](adr/). Pairs: [conversion-matrix.md](conversion-matrix.md).

## Stack
.NET 10, Avalonia 12 (12.1.6 on NuGet, MIT). UI app published with Native AOT; Workers are self-contained, trimmed, not AOT ([ADR 0001](adr/0001-avalonia-net10-aot-ui-only.md)). Installer: Inno Setup, per-user (`PrivilegesRequired=lowest`); portable ZIP. Tests: xUnit via `dotnet test`. Build: `dotnet build`.

## Engines and licences
Data from primary sources (NuGet pages, official repos), checked 2026-11-10 by a research pass. **n.v.** = not verified; the check is a to-do in Ticket 0 or in the ticket that adds the library. Sizes are NuGet package sizes, not final installed size.

### MVP (built-in)

| Family | Library | Licence | Version | Size | Trimming (Worker) | Notes |
|---|---|---|---|---|---|---|
| App | Avalonia | MIT | 12.1.6 | 9.7 MB pkg | AOT documented by Avalonia (needs `IsAotCompatible`, trimmer roots) | |
| Images | NetVips | MIT | 3.2.0 | 0.26 MB | n.v. | wrapper |
| Images | NetVips.Native.win-x64 (libvips) | **LGPL-3.0-or-later** (libvips itself: LGPL-2.1+) | 8.18.7 | 7.7 MB | n.v. | dynamic link; ship licence + source offer. Declares JPEG, PNG, WebP, AVIF, TIFF, GIF, SVG(in). **Not confirmed:** AVIF encode, GIF via cgif, HEIF/HEVC backend, `vips_block_untrusted_set`, which loaders are compiled in |
| Images | WIC (Windows) | OS component | – | 0 | – | HEIC read, best effort, user's HEVC extension |
| Data | Sep | MIT | 0.17.1 | n.v. | README: trimmable + AOT | candidate CSV |
| Data | CsvHelper | MS-PL OR Apache-2.0 | 34.0.0 | 1.1 MB | `IsTrimmable` + `IsAotCompatible` (net8/net10) | candidate CSV; avoid `dynamic`/Linq |
| Data | System.Text.Json, System.Xml | MIT (BCL) | .NET 10 | 0 | trim-safe with source generators for JSON | DTD/external entities off |
| Data | YamlDotNet | MIT | 18.1.0 | 0.78 MB | n.v. | safe mode, no arbitrary types |
| Data | MiniExcel | Apache-2.0 | 1.46.0 | 0.98 MB | n.v. | streaming; repo is `mini-software/MiniExcel` |
| Text | Markdig | BSD-2-Clause | 1.6.0 | 1.3 MB | n.v. | |
| Text | ReverseMarkdown | MIT | 6.3.0 | 0.53 MB | declared AOT/trimming ready | |
| Text | HtmlSanitizer | MIT | 11.0.26 | 0.18 MB | n.v. | depends on AngleSharp |

CSV choice: **Sep** if it passes the trimming publish test (fast, MIT, AOT-friendly), else CsvHelper. Decided in the data ticket.
Every "n.v." trimming entry must pass a `dotnet publish` of the Worker with trimming and warnings-as-errors before it is adopted.

### Brand assets

| Asset | Licence | Notes |
|---|---|---|
| Bricolage Grotesque (logotype "kalei") | SIL OFL 1.1, "Copyright 2022 The Bricolage Grotesque Project Authors"; no Reserved Font Name declared | Read from upstream repo `ateliertriay/bricolage` (README + `OFL.txt`). Google Fonts page could not be read, so confirmed from upstream only. Upstream says it is a fork of Mayenne Sans (its licence not checked). Variable TTF ≈ 399 KB; the app embeds a **subset** (letters of the logotype) and never loads it from the network. Ship `OFL.txt` in third-party notices |

### Later modules (not in v1)

| Module | Engine | Licence | Status / finding |
|---|---|---|---|
| PDF render | PDFium via PDFiumCore 157.0.8100 | Apache-2.0 (binding), BSD-3 (PDFium) | OK. Avoid PdfiumViewer: archived 2019 |
| PDF create/edit | PDFsharp | **6.2.2 (2025-09-22): MIT** per NuGet. **6.5.0 (2026-10-13/14) and 7.0 preview: AGPL-3.0 dual (AGPL or commercial)**, 6.5.0+ code must call `PdfSharpLicense.SetAgplLicense()` | **Latest stable is not usable.** Only an old pinned 6.2.x could be MIT (6.2.3 and 6.2.4 licence fields not verified), with no upstream fixes after the switch. Decide when the PDF module is planned: pinned 6.2.x vs another library. Sources: nuget.org/packages/PDFsharp (+ /6.2.2, /6.5.0), github.com/empira/PDFsharp `master/LICENSE` ("AGPL Dual License"), docs.pdfsharp.net/General/License/License.html and /General/History.html ("Beginning with version 6.5.0, your code must select the type of license"). Samples stay MIT |
| PDF optimise | qpdf | Apache-2.0 | CLI, separate process; version n.v. |
| Audio/video | FFmpeg, BtbN "lgpl" build | LGPL (build flavour; licence text not stated on the page) | gyan.dev builds are **GPLv3 only**, unusable. Not confirmed in BtbN LGPL build: libvpx, SVT-AV1, libopus, libmp3lame, `h264_mf` |
| Documents | pandoc 3.13 | GPL-2.0-or-later | separate process; ~43 MB; ship licence + source offer |
| Archives | SharpCompress | MIT (SPDX n.v.) | version n.v. |
| Archives | 7-Zip 26.06 (7za) | LGPL-2.1+ with unRAR restriction | no RAR |
| Vector | resvg | Apache-2.0 OR MIT | no .NET binding found: separate binary |
| 3D | Assimp | BSD-3-Clause (modified; SPDX n.v.) | |
| Signing | SignPath Foundation | free for OSS (claimed) | eligibility terms **not read**: verify before relying on it |
| Installer | Inno Setup | permissive licence text, but the site asks commercial users to buy a licence | verify the exact `LICENSE.TXT` of the version used; alternative if unsuitable |

## Corrections to earlier assumptions
1. NetVips.Native is LGPL-**3.0**+, not 2.1+.
2. PDFsharp is MIT only up to 6.2.x; from 6.5.0 it is AGPL/commercial → latest version removed from the plan (see row above).
3. resvg is Apache-2.0 OR MIT, not MPL.
4. gyan.dev has no LGPL FFmpeg build; BtbN does.
5. libvips offers a restricted "web" Windows build (safe loaders only) and an "all" build; which one NetVips.Native resembles must be checked in Ticket 0. Matters because Kalei opens untrusted images.

## Open technical checks (go to Ticket 0 or the owning ticket)
- AppContainer + .NET runtime + NetVips.Native + WIC working together; handle passing into AppContainer.
- NetVips.Native content: AVIF write, GIF via cgif, HEVC absent, loaders list, block-untrusted API.
- Real installer size against the PRD target (~45 MB).
- Trimming test for each library marked n.v.
- Redistribution notices: THIRD-PARTY-NOTICES generated at build, shown in About.
