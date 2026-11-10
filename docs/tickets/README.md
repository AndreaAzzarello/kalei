# Tickets for Kalei 1.0 (Windows)

Local tickets, written as vertical slices (each one delivers something that can be run or checked end to end) from the [1.0 spec](../spec/1.0-windows.md). They are plain files until the project has an issue tracker (`/setup-matt-pocock-skills` at repository set-up); then they are published as they are.

**Status**: `ready-for-agent` can be done by an agent alone; `needs-owner-hardware` needs the owner's virtual machines or devices (see [reference machines](../ticket-0.md#reference-machines)).
**Size**: S = one context window, M = about two, L = three or more (split at the start if needed).
**Rules**: the security requirements S1–S12 of [architecture](../architecture.md#security-requirements-normative-the-only-place-they-are-written) that the ticket must satisfy.

Starting points (nothing blocks them): [01](01-sandbox-spike.md), [02](02-image-engine-content.md), [03](03-build-and-size-spike.md), [04](04-in-app-donut-spike.md), [35](35-linux-spike.md), [36](36-macos-spike.md).
Critical path: 01 → 07 → 09 → 10 → 12 → 33. Tickets 35 and 36 (Linux, macOS) and 34 (global gesture) do not block 1.0.

## Ticket 0: Windows spike (blocks 1.0)
| # | Title | Blocked by | Size | Rules | Status |
|---|---|---|---|---|---|
| [01](01-sandbox-spike.md) | Sandbox spike: AppContainer + .NET + libvips from inherited handles, network gate | – | L | S1, S3, S5, S7 | needs-owner-hardware |
| [02](02-image-engine-content.md) | Image engine content: libvips build, BMP and ICO, HEIC through WIC | – | M | S3 | ready-for-agent |
| [03](03-build-and-size-spike.md) | Build spike: Native AOT, trimmed Worker, installer size, antivirus, licences | – | L | none | needs-owner-hardware |
| [04](04-in-app-donut-spike.md) | In-app Donut spike: drag over an Avalonia window, latency, wheel, two monitors | – | M | S3 | needs-owner-hardware |

## Ticket 1: core and first vertical flow
| # | Title | Blocked by | Size | Rules | Status |
|---|---|---|---|---|---|
| [05](05-solution-skeleton.md) | Solution skeleton: projects, platform adapter interfaces, quality gates | 03 | M | S10 | ready-for-agent |
| [06](06-header-detection-images.md) | Format detection from the header for images | 05 | S | S3 | ready-for-agent |
| [07](07-launcher-and-worker-ipc.md) | Windows sandbox launcher and Worker IPC host | 01, 05 | L | S1, S2, S5 | ready-for-agent |
| [08](08-broker-and-output-writing.md) | Broker and Output writing: safe temporary file, journal, naming | 05 | M | S4, S7 | ready-for-agent |
| [09](09-images-module.md) | Images Module: the confirmed set from Core to file | 02, 06, 07, 08 | L | S2, S3, S5 | ready-for-agent |
| [10](10-main-window-single-file.md) | Main window: convert one file from drop to Done | 09 | L | S3, S10 | ready-for-agent |
| [11](11-donut-library.md) | Donut library: geometry, hit-testing, faces, format intersection | 05 | M | none | ready-for-agent |
| [12](12-in-app-donut-host.md) | In-app Donut host: drag over the window, release on a Slice | 04, 10, 11 | L | S3 | ready-for-agent |

## Rest of 1.0
| # | Title | Blocked by | Size | Rules | Status |
|---|---|---|---|---|---|
| [13](13-probe-and-preview.md) | Probe and Preview in the Worker: safe previews and thumbnails | 07, 10 | M | S3 | ready-for-agent |
| [14](14-batch-and-folders.md) | Batch and dropped folders | 07, 10 | L | S2, S4 | ready-for-agent |
| [15](15-name-clash-and-output-folder.md) | Name clash choice and Output folder choice | 08, 10 | S | S4 | ready-for-agent |
| [16](16-images-avif-gif.md) | Images: AVIF and GIF writing | 02, 09 | S | S2 | ready-for-agent |
| [17](17-images-bmp-ico.md) | Images: BMP and ICO codecs | 02, 09 | M | S2, S3 | ready-for-agent |
| [18](18-images-heic-via-wic.md) | Images: HEIC reading through WIC | 02, 09 | M | S3, S5 | ready-for-agent |
| [19](19-text-like-detection.md) | Detection of text-like Formats in the Probe | 06, 07 | M | S3, S9 | ready-for-agent |
| [20](20-data-tabular.md) | Data: CSV, TSV and XLSX | 07, 19 | L | S2, S9 | ready-for-agent |
| [21](21-data-tree.md) | Data: JSON, YAML and XML | 07, 19 | M | S2, S9 | ready-for-agent |
| [22](22-json-tabular.md) | Data: JSON to and from tables | 20, 21 | M | S9 | ready-for-agent |
| [23](23-text-module.md) | Text: TXT, Markdown and HTML | 07, 19 | M | S9 | ready-for-agent |
| [24](24-settings-and-persistence.md) | Settings and persistence | 10 | M | S10 | ready-for-agent |
| [25](25-localisation-and-error-catalogue.md) | Localisation (English and Italian) and the error catalogue | 10 | M | S10 | ready-for-agent |
| [26](26-logging-policy.md) | Logging: format, rotation and the no-paths test | 05 | S | S10 | ready-for-agent |
| [27](27-browser-drops-and-origin-mark.md) | Drops from a browser and the origin mark | 08, 10 | M | S7, S12 | ready-for-agent |
| [28](28-limits-page.md) | Settings: Limits page | 14, 24 | S | S2 | ready-for-agent |
| [29](29-accessibility-pass.md) | Accessibility pass | 12, 24 | M | none | ready-for-agent |

## Release
| # | Title | Blocked by | Size | Rules | Status |
|---|---|---|---|---|---|
| [30](30-portable-zip-sums-notices.md) | Portable ZIP, SHA256SUMS, third-party notices and About | 03, 10 | M | S6 | ready-for-agent |
| [31](31-per-user-installer.md) | Per-user installer and clean uninstall | 01, 03, 30 | L | S1, S5 | needs-owner-hardware |
| [32](32-code-signing.md) | Code signing, only if SignPath is confirmed | 31 | S | none | ready-for-agent |
| [33](33-final-verification.md) | Final verification: the S1–S12 gate and a release dry run | 13, 15, 16, 17, 18, 22, 23, 25, 26, 27, 28, 29, 32 | L | S1–S12 | needs-owner-hardware |

## Spikes that do not block 1.0
| # | Title | Blocked by | Size | Rules | Status |
|---|---|---|---|---|---|
| [34](34-global-gesture-spike.md) | Spike (1.2): global gesture, Sentinel, false triggers | 12 | L | S11 | needs-owner-hardware |
| [35](35-linux-spike.md) | Spike: Linux sandbox, Donut and packaging (L1–L6) | – | L | S1, S5 | needs-owner-hardware |
| [36](36-macos-spike.md) | Spike: macOS sandbox, Gatekeeper and Donut (M1–M6) | – | L | S1, S5 | needs-owner-hardware |

