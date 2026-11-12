# Kalei — Product Requirements

*Kalei — convert any file, privately.*

## What it is
A small Windows desktop app that converts files between formats. Everything happens on the user's PC: no upload, no telemetry, no account. See [GLOSSARY.md](../GLOSSARY.md) for terms.

## Who it is for
General, non-technical users who need a quick, trustworthy conversion (a photo to WebP, a spreadsheet to CSV, Markdown to HTML) and do not want to send files to a website. UI in English and Italian (follows the system language). Open source, MIT.

## Principles
1. **Local**: files never leave the PC. Kalei 1.0 makes no network connection at all; from 1.1 the only allowed one is installing an Optional module the user approved.
2. **Private**: zero telemetry; logs are local and contain no file names or paths.
3. **Modular**: a light Core; each family of formats is a Module; from 1.1 heavy Modules are installed only with consent.
4. **Simple**: one window, a drop zone, sensible defaults; advanced options hidden in an "Advanced" panel.
5. **Safe**: untrusted files are parsed in a sandboxed Worker (see [architecture](architecture.md), requirements S1–S12).

## Scope of 1.0 (the MVP; "MVP", "v1" and "1.0" all mean this release)
Platform: **Windows 10 22H2 and Windows 11, x64 only.** The architecture is multiplatform from the start ([ADR 0010](adr/0010-multiplatform-architecture-windows-first.md)); Linux and macOS come in 1.x.
- Built-in Modules: **Images** (PNG, JPG, WebP, GIF, BMP, TIFF, ICO, AVIF, plus HEIC read-only best effort via the user's own WIC extensions), **Data** (CSV, TSV, JSON, XML, YAML, XLSX), **Text** (TXT, MD, HTML). **Only the formats confirmed by the feasibility spike ship in 1.0** (see [ticket-0.md](ticket-0.md) and the matrix); anything not confirmed moves to a later release.
- **In-app Donut**: dragging files over Kalei's window opens a ring of Slices at the cursor with only the suitable target Formats; releasing on a Slice converts with no further window ([ADR 0008](adr/0008-radial-donut-component.md)). Releasing elsewhere in the window gives the normal drop-zone flow. Always has a keyboard/screen-reader equivalent in the main window.
- Files dragged from a browser: only the bytes the browser itself hands over are accepted. Kalei never downloads a link ("Save the file first"). Converted files inherit the source's Mark of the Web (Windows `Zone.Identifier`).
- Single and **Batch** conversion, including dropped folders (recursive, structure preserved). **In 1.0 a Batch has one target Format for all its files.** The Donut and the target list offer only the Formats that every file can be converted to or already is (a file already in the chosen Format is skipped, with its reason); if there are none, Kalei asks the user to split the Batch ("These files have no format in common. Convert them in separate groups."). One target per Category comes in 1.1.
- Only sensible target Formats offered, most common preselected.
- Preview of the Source file (generated in a Worker, never by the UI process); progress bar; clear errors; cancel at any time. There is **no size Estimate in 1.0**: the real size is shown when the Conversion is done.
- Output next to the Source file or in a chosen folder; name clash → `name (1).ext`, never overwrite; optional "ask every time" with "apply to all".
- Accent colour and icon follow the Category of the dropped file (soft transition, can be turned off, respects "Reduce animations").
- Light/dark theme following the system.
- Installer (per-user, no admin) and portable ZIP on GitHub Releases.

## Release order (the single source for version numbers; other documents point here)
- **1.0**: Windows. Window + Drop zone + In-app Donut, built-in Modules only.
- **1.1**: Optional-module install flow, shipped together with the first Optional module (consent, size, fixed source, per-file hash check, offline `.kmod`, uninstall); one target Format per Category in a Batch; XLSX "all sheets" (one Output file per sheet, which needs Conversions with several Output files); Explorer context-menu entry and the `kalei` command line (both reuse the Core and the sandbox); recent conversions with local thumbnails generated in a Worker (can be turned off).
- **1.x (Linux, macOS)**: each OS only after its own spike (not blocking 1.0) and with the same release gate S5 (no network for Workers): where the OS sandbox cannot guarantee it, the promise is not made for that OS. The In-app Donut works on every OS.
- **1.2**: Global Donut at the cursor, opt-in, only in a code-signed build ([ADR 0009](adr/0009-mouse-only-hook-and-sentinel.md)), only where the OS allows it (Windows; macOS with Input Monitoring or Accessibility permission; Linux X11 only, not Wayland).
- After 1.2: crop before converting (a deliberate exception to "editing files" being out of scope), watch folders (opt-in), further Modules.

## Out of scope for 1.0
PDF, audio/video, Office, ebooks, archives, vector, 3D, fonts (all later Modules, roughly in that order); macOS and Linux builds (1.x); Optional-module installation, per-Category targets, Explorer context menu, command-line interface and recent conversions (1.1); Global Donut, resident tray process and start at sign-in (1.2); size Estimate; automatic updates; deleting or moving Source files; cloud features of any kind; batch renaming; OCR; editing files; annotate/background/metadata editing tools; downloading links from a browser; XLSX "all sheets" (1.1); the Donut's back Face (needs a Module with more than 8 targets).

## Differences from similar tools
Kalei is inspired by Filee (https://github.com/KnifeLemon/Filee, MIT), which pioneered the radial drop-to-convert gesture ([ADR 0007](adr/0007-independent-from-filee.md)). The README may claim only what can be checked: parsing in sandboxed Workers without network (**only for an OS where the feasibility spike proves the sandbox blocks the network, release gate S5; otherwise the claim is removed for that OS**), no connection started by Kalei (from 1.1: except an approved Module install), no automatic update check, no GPL code linked into any Kalei process (GPL tools may ship as separate programs, see [ADR 0005](adr/0005-mit-license-copyleft-engines-as-separate-processes.md)). For the global gesture: *"Kalei never records keystrokes. During a drag it only checks whether your chosen modifier key is held."* Italian-friendly data (decimal commas, Windows-1252, `;` separators) is a quality feature, not a slogan.

## Defaults and limits
The limits table (limit, default, whether editable, hard maximum, enforcing component, error code) lives in [architecture.md](architecture.md#limits); the values are defaults, and the editable ones can be changed in Settings → Limits up to a hard maximum. Metadata: GPS removed by default; EXIF orientation applied to pixels; option to strip all metadata.

## Success criteria
Thresholds and measuring method are in [ticket-0.md](ticket-0.md). **Reference machine**: a Hyper-V virtual machine with Windows 10 22H2, 2 vCPU, 4 GB RAM and a virtual SSD (the worst case, reproducible); startup, memory and Donut latency are measured there and confirmed on a Windows 11 desktop PC.
- Installer: target ≤ 45 MB, acceptable ≤ 60 MB; above that the scope is reviewed. Cold start ≤ 1.3 s (median of 10) on the reference machine, with the UI published as ReadyToRun + trimming (threshold revised from 1 s on 2026-11-12 after measuring, see [ticket-0](ticket-0.md#w7--decision-after-ticket-03c-2026-11-12) and ADR 0001; Native AOT is an optional trial).
- A user converts a file in at most three actions (drop, check target, Convert).
- **Conditional on gate S5:** a TCP connect and a DNS lookup from inside a Worker both fail (sandbox test); if the gate fails for an OS, this criterion and the matching README claim are removed for that OS.
- Malformed, oversized and mislabeled files fail cleanly, with a message, and leave no temporary files (checked by a sample per Format, including corrupt, oversized and wrong-extension files).
