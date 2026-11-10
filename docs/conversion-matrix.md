# Kalei — Conversion matrix

Every supported *from → to* pair and the Module that handles it. Terms: [GLOSSARY.md](../GLOSSARY.md). Engines and licences: [TRD](TRD.md).
Status: **1.0** = built into the first release · **Later** = planned Optional module (1.1 onwards) · ✗ = not supported. Release numbers: [PRD](PRD.md#release-order-the-single-source-for-version-numbers-other-documents-point-here).
Pairs not listed are not offered in the UI. Input is recognised from content, not extension.

## Images — Module `images` (1.0, built-in, libvips + WIC)

**Every image pair below ships in 1.0 only if the Ticket 0 spike confirms the engine can do it** (AVIF writing, GIF via cgif, BMP and ICO codecs, HEIC; see [ticket-0.md](ticket-0.md)). Formats not confirmed move to a later release. Category: **image**.

| From ↓ / To → | PNG | JPG | WebP | GIF | BMP | TIFF | ICO | AVIF |
|---|---|---|---|---|---|---|---|---|
| PNG | – | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| JPG | ✓ | – | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| WebP | ✓ | ✓ | – | ✓ | ✓ | ✓ | ✓ | ✓ |
| GIF | ✓ | ✓ | ✓ | – | ✓ | ✓ | ✓ | ✓ |
| BMP | ✓ | ✓ | ✓ | ✓ | – | ✓ | ✓ | ✓ |
| TIFF | ✓ | ✓ | ✓ | ✓ | ✓ | – | ✓ | ✓ |
| ICO (read) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | – | ✓ |
| AVIF | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | – |
| HEIC (read only, best effort via WIC) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |

Decided behaviour: animated inputs (GIF, WebP, AVIF) convert their first frame only, and the Donut/target list shows a note "first frame only" for them; transparency converted to JPG/BMP is flattened onto white; ICO output holds the sizes 16, 32, 48 and 256 px (smaller ones are not upscaled; an input larger than 256 px is scaled down), and as an input the largest image of the ICO is used; HEIC is input only and works when the user's Windows has the required WIC extensions (which extensions are needed is checked in Ticket 0), otherwise a clear message is shown. Options: quality, max size/resolution, metadata (GPS removed by default; orientation applied to pixels).

## Data — Module `data` (1.0, built-in)

Two groups. Within a group every pair is supported.

| Group | Formats | Category (accent, icon) | Within group |
|---|---|---|---|
| Tabular | CSV, TSV, XLSX | spreadsheet | all pairs ✓ |
| Tree | JSON, YAML, XML | document | all pairs ✓ |

Across groups: **JSON ↔ CSV / TSV / XLSX** only (JSON must be an array of objects when going to tabular; nested values flattened with dot notation, arrays as `a.0`, `a.1` or joined with a configurable separator). YAML and XML to tabular are not offered in v1.

Rules: CSV separator (`,` `;` tab) and encoding (UTF-8/BOM, Windows-1252) auto-detected, overridable in Advanced; Italian numbers (`1.234,56`) understood when converting to JSON/XLSX; XLSX default first sheet, option "all sheets" (one CSV per sheet); XLSX formulas export the cached value; anything written into XLSX is a value or text, never a formula; XML convention `@attribute` / `#text`; XML DTD and external entities rejected. JSON going to tabular must be an array of objects (an array of plain values becomes one column named `value`; a ragged set of objects gives the union of keys with empty cells); CSV/TSV output neutralises cells starting with `=`, `+`, `-` or `@` (default on, switch in Options, see S9 in [architecture](architecture.md)); Windows-1252 output replaces characters it cannot hold with `?` and warns.

## Text — Module `text` (1.0, built-in)

Category: **document** for TXT, MD and HTML.

| From ↓ / To → | TXT | MD | HTML |
|---|---|---|---|
| TXT | ✓ (encoding, line endings) | ✗ | ✗ |
| MD | ✓ | – | ✓ (full page, minimal CSS, sanitised) |
| HTML | ✓ | ✓ | – |

## Donut faces

The Donut ([ADR 0008](adr/0008-radial-donut-component.md)) takes its targets from the tables above, excluding the Source format. The split between front and back Face is a UI decision and lives in the [design brief](design-brief.md#donut-faces).

## Later modules (not in v1; not final)

| Module | Formats | Notes |
|---|---|---|
| `pdf` | PDF ↔ images, text → PDF | engines and open choices: see the PDF section of the [TRD](TRD.md) |
| `av` | MP3, WAV, FLAC, OGG/Opus, AAC, MP4, WebM, MKV, GIF | FFmpeg LGPL build |
| `documents` | DOCX, ODT, RTF, EPUB, MD, HTML | pandoc; LibreOffice only if already installed |
| `archives` | ZIP, TAR, GZ, 7Z (create/extract) | ratio and size limits |
| `vector` | SVG → PNG/PDF | resvg |
| `3d` | OBJ, STL, GLB, FBX… | Assimp |
| `fonts` | TTF, OTF, WOFF, WOFF2 | to be defined |

Each Module's `module.json` declares its pairs; this document is regenerated from those manifests when a Module is added.
