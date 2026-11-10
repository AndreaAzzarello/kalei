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
- **In-app Donut**: dragging files over Kalei's window opens a ring of Slices at the cursor with only the suitable target Formats; releasing on a Slice converts with no further window ([ADR 0008](adr/0008-radial-donut-component.md)). Releasing elsewhere in the window gives the normal drop-zone flow. Always has a keyboard/screen-reader equivalent in the main window.
- Files dragged from a browser: only the bytes the browser itself hands over are accepted. Kalei never downloads a link ("Save the file first"). Converted files inherit the source's Mark of the Web (Windows `Zone.Identifier`).
- Single and **Batch** conversion, including dropped folders (recursive, structure preserved).
- Only sensible target Formats offered, most common preselected.
- Preview of the Source file and approximate size Estimate before converting; progress bar; clear errors; cancel at any time.
- Output next to the Source file or in a chosen folder; name clash → `name (1).ext`, never overwrite; optional "ask every time" with "apply to all".
- Accent colour and icon follow the Category of the dropped file (soft transition, can be turned off, respects "Reduce animations").
- Light/dark theme following the system.
- Optional-module install flow (consent, size, fixed source, hash check, offline `.kmod`, uninstall).
- Installer (per-user, no admin) and portable ZIP on GitHub Releases.

## Release order
- **1.0**: window + Drop zone + In-app Donut (this document's MVP scope).
- **1.1**: Explorer context-menu entry and the `kalei` command line (both reuse the Core and the sandbox); recent conversions with local thumbnails (can be turned off).
- **1.2**: Global Donut at the cursor, opt-in, only in a code-signed build ([ADR 0009](adr/0009-mouse-only-hook-and-sentinel.md)).
- After 1.2: crop before converting, watch folders (opt-in), further Modules.

## Out of scope for 1.0
PDF, audio/video, Office, ebooks, archives, vector, 3D, fonts (all later Modules, roughly in that order); macOS/Linux; Global Donut, resident tray process and start at sign-in (1.2); Explorer context menu and command-line interface (1.1); automatic updates; deleting or moving Source files; cloud features of any kind; batch renaming; OCR; editing files; annotate/background/metadata editing tools; downloading links from a browser.

## Differences from similar tools
Kalei is inspired by Filee (https://github.com/KnifeLemon/Filee, MIT), which pioneered the radial drop-to-convert gesture ([ADR 0007](adr/0007-independent-from-filee.md)). The README may claim only what can be checked: parsing in sandboxed Workers without network (once Ticket 0 proves it), no connection started by Kalei except an approved Module install, no automatic update check, only permissive or LGPL engines in the install. For the global gesture: *"Kalei never records keystrokes. During a drag it only checks whether your chosen modifier key is held."* Italian-friendly data (decimal commas, Windows-1252, `;` separators) is a quality feature, not a slogan.

## Defaults and limits
Per-Conversion limits (changeable in Settings → Advanced): 2 GB per file, 250 megapixels, 10,000 files per Batch, folder depth 32, 2 GB RAM per Worker, 2-minute timeout for images and data. Metadata: GPS removed by default; EXIF orientation applied to pixels; option to strip all metadata.

## Success criteria
- Installer under ~45 MB (to be measured in Ticket 0); cold start under 1 s on a mid-range PC.
- A user converts a file in at most three actions (drop, check target, Convert).
- No network packet leaves a Worker (verified by sandbox test).
- Malformed, oversized and mislabeled files fail cleanly, with a message, and leave no temporary files.
