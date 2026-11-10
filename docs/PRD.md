# Kalei — Product Requirements

*Kalei — convert any file, privately.*

## What it is
A small Windows desktop app that converts files between formats. Everything happens on the user's PC: no upload, no telemetry, no account. See [GLOSSARY.md](../GLOSSARY.md) for terms.

## Who it is for
General, non-technical users who need a quick, trustworthy conversion (a photo to WebP, a spreadsheet to CSV, Markdown to HTML) and do not want to send files to a website. UI in English and Italian (follows the system language). Open source, MIT.

## Principles
1. **Local**: files never leave the PC. The only allowed network use is installing an Optional module the user approved.
2. **Private**: zero telemetry; logs are local and contain no file names or paths.
3. **Modular**: a light Core; each family of formats is a Module; heavy Modules are installed only with consent.
4. **Simple**: one window, a drop zone, sensible defaults; advanced options hidden in an "Advanced" panel.
5. **Safe**: untrusted files are parsed in a sandboxed Worker (see [architecture](architecture.md)).

## MVP scope (v1, Windows x64)
- Built-in Modules: **Images** (PNG, JPG, WebP, GIF, BMP, TIFF, ICO, AVIF read+write; HEIC best effort via the user's own WIC HEVC extension), **Data** (CSV, TSV, JSON, XML, YAML, XLSX), **Text** (TXT, MD, HTML).
- Single and **Batch** conversion, including dropped folders (recursive, structure preserved).
- Only sensible target Formats offered, most common preselected.
- Preview of the Source file and approximate size Estimate before converting; progress bar; clear errors; cancel at any time.
- Output next to the Source file or in a chosen folder; name clash → `name (1).ext`, never overwrite; optional "ask every time" with "apply to all".
- Accent colour and icon follow the Category of the dropped file (soft transition, can be turned off, respects "Reduce animations").
- Light/dark theme following the system.
- Optional-module install flow (consent, size, fixed source, hash check, offline `.kmod`, uninstall).
- Installer (per-user, no admin) and portable ZIP on GitHub Releases.

## Out of scope for v1
PDF, audio/video, Office, ebooks, archives, vector, 3D, fonts (all later Modules, roughly in that order); macOS/Linux; Explorer context menu; command-line interface; automatic updates; deleting or moving Source files; cloud features of any kind; batch renaming; OCR; editing files.

## Defaults and limits
Per-Conversion limits (changeable in Settings → Advanced): 2 GB per file, 250 megapixels, 10,000 files per Batch, folder depth 32, 2 GB RAM per Worker, 2-minute timeout for images and data. Metadata: GPS removed by default; EXIF orientation applied to pixels; option to strip all metadata.

## Success criteria
- Installer under ~45 MB (to be measured in Ticket 0); cold start under 1 s on a mid-range PC.
- A user converts a file in at most three actions (drop, check target, Convert).
- No network packet leaves a Worker (verified by sandbox test).
- Malformed, oversized and mislabeled files fail cleanly, with a message, and leave no temporary files.
