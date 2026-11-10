# Kalei — Conversion matrix

Every supported *from → to* pair and the Module that handles it. Terms: [GLOSSARY.md](../GLOSSARY.md). Engines and licences: [TRD](TRD.md).
Status: **MVP** = built into v1 · **Later** = planned Optional module · ✗ = not supported.
Pairs not listed are not offered in the UI. Input is recognised from content, not extension.

## Images — Module `images` (MVP, built-in, libvips + WIC)

| From ↓ / To → | PNG | JPG | WebP | GIF | BMP | TIFF | ICO | AVIF |
|---|---|---|---|---|---|---|---|---|
| PNG | – | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| JPG | ✓ | – | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| WebP | ✓ | ✓ | – | ✓ | ✓ | ✓ | ✓ | ✓ |
| GIF | ✓ | ✓ | ✓ | – | ✓ | ✓ | ✓ | ✓ |
| BMP | ✓ | ✓ | ✓ | ✓ | – | ✓ | ✓ | ✓ |
| TIFF | ✓ | ✓ | ✓ | ✓ | ✓ | – | ✓ | ✓ |
| ICO (read only) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | – | ✓ |
| AVIF | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | – |
| HEIC (read only, best effort via WIC) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |

Notes (v1 behaviour, to be confirmed in the spec): animated inputs (GIF, WebP, AVIF) convert their first frame only; transparency converted to JPG/BMP is flattened onto white; ICO output holds standard sizes up to 256 px; HEIC works only when the user's Windows has the HEVC extension, otherwise a clear message is shown. Options: quality, max size/resolution, metadata (GPS removed by default; orientation applied to pixels).

## Data — Module `data` (MVP, built-in)

Two groups. Within a group every pair is supported.

| Group | Formats | Within group |
|---|---|---|
| Tabular | CSV, TSV, XLSX | all pairs ✓ |
| Tree | JSON, YAML, XML | all pairs ✓ |

Across groups: **JSON ↔ CSV / TSV / XLSX** only (JSON must be an array of objects when going to tabular; nested values flattened with dot notation, arrays as `a.0`, `a.1` or joined with a configurable separator). YAML and XML to tabular are not offered in v1.

Rules: CSV separator (`,` `;` tab) and encoding (UTF-8/BOM, Windows-1252) auto-detected, overridable in Advanced; Italian numbers (`1.234,56`) understood when converting to JSON/XLSX; XLSX default first sheet, option "all sheets" (one CSV per sheet); XLSX formulas export the cached value; anything written into XLSX is a value or text, never a formula; XML convention `@attribute` / `#text`; XML DTD and external entities rejected.

## Text — Module `text` (MVP, built-in)

| From ↓ / To → | TXT | MD | HTML |
|---|---|---|---|
| TXT | ✓ (encoding, line endings) | ✗ | ✗ |
| MD | ✓ | – | ✓ (full page, minimal CSS, sanitised) |
| HTML | ✓ | ✓ | – |

## Later modules (not in v1; not final)

| Module | Formats | Notes |
|---|---|---|
| `pdf` | PDF ↔ images, text → PDF | PDFium, qpdf; PDF creation library to be chosen (PDFsharp is AGPL, see [TRD](TRD.md)) |
| `av` | MP3, WAV, FLAC, OGG/Opus, AAC, MP4, WebM, MKV, GIF | FFmpeg LGPL build |
| `documents` | DOCX, ODT, RTF, EPUB, MD, HTML | pandoc; LibreOffice only if already installed |
| `archives` | ZIP, TAR, GZ, 7Z (create/extract) | ratio and size limits |
| `vector` | SVG → PNG/PDF | resvg |
| `3d` | OBJ, STL, GLB, FBX… | Assimp |
| `fonts` | TTF, OTF, WOFF, WOFF2 | to be defined |

Each Module's `module.json` declares its pairs; this document is regenerated from those manifests when a Module is added.
