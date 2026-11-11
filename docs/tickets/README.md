# Tickets for Kalei 1.0 (Windows)

Local tickets, written as vertical slices (each one delivers something that can be run or checked end to end) from the [1.0 spec](../spec/1.0-windows.md). They are published as GitHub issues #1–#44 (the first number in each row is the file, the second the issue; the issue carries the same text with real issue numbers in "Blocked by"). The files stay as the source for the text. There are 48 tickets: 01–39 with a few split into a and b, 07c added after the sandbox spike and 03c (issue #56) after the reference-VM results.

**Words used here**: "owner" is the owner of the repository; "agent" is an AI coding agent; WIC is the Windows Imaging Component; AOT is ahead-of-time compilation; n.v. is "not verified" (see the [TRD](../TRD.md)); "Ticket 0" in the other documents means the [feasibility spike document](../ticket-0.md) and the spike tickets that carry out its proofs (01–04 for Windows, 35 Linux, 36 macOS, 34 global gesture). "Main reference machine" is the Hyper-V Windows 10 VM and "Windows 11 PC" is the owner's desktop PC ([reference machines](../ticket-0.md#reference-machines)).

**Status**: `ready-for-agent` can be done by an agent alone; `needs-owner-hardware` needs the owner's virtual machines or devices. A ticket with a **Manual check** line has acceptance criteria that a person must look at; the rest is automatic.
**Size**: S = one context window, M = about two, L = three or more (split at the start if needed).
**Rules**: the security requirements of [architecture](../architecture.md#security-requirements-normative-the-only-place-they-are-written), in short:
S1 one Worker per Conversion · S2 limits and bomb protection · S3 type from content, Core reads headers only, parsers run in Workers · S4 safe output names, no overwrite, Source untouched · S5 no network for Workers (release gate) · S6 no automatic update or check · S7 temporary files only in Kalei's folder · S8 module integrity · S9 safe parsers and writers · S10 logs without paths · S11 mouse-only hook · S12 browser drops.

Starting points (nothing blocks them): [01](01-sandbox-spike.md), [02](02-image-engine-content.md), [03a](03a-build-spike.md), [03b](03b-licence-and-signing-checks.md), [03c](03c-w7-cold-start-breakdown.md), [04](04-in-app-donut-spike.md), [35](35-linux-spike.md), [36](36-macos-spike.md).
Longest chain of dependencies: [03a](03a-build-spike.md) → [05](05-solution-skeleton.md) → [07a](07a-ipc-protocol-and-worker-host.md) → [07b](07b-windows-sandbox-launcher.md) → [09a](09a-images-module-core.md) → [10a](10a-main-window-file-card.md) → [10b](10b-main-window-convert.md) → [15](15-name-clash-and-output-folder.md) → [14a](14a-batch-engine.md) → [14b](14b-batch-window.md) → [25](25-localisation-and-error-catalogue.md) → [33](33-final-verification.md). Tickets 34, 35 and 36 do not block 1.0.

## Windows spike (blocks 1.0)
| # | Title | Blocked by | Size | Rules | Status | Manual check |
|---|---|---|---|---|---|---|
| [01](01-sandbox-spike.md) · [#1](https://github.com/AndreaAzzarello/kalei/issues/1) | Sandbox spike: AppContainer + .NET + libvips from inherited handles, network gate | – | M | S1, S3, S5, S7 | needs-owner-hardware | – |
| [02](02-image-engine-content.md) · [#2](https://github.com/AndreaAzzarello/kalei/issues/2) | Image engine content: libvips build, BMP and ICO, HEIC through WIC | – | M | S3 | ready-for-agent | – |
| [03a](03a-build-spike.md) · [#3](https://github.com/AndreaAzzarello/kalei/issues/3) | Build spike: Native AOT, trimmed Worker, CSV library, first size estimate | – | M | none | needs-owner-hardware | – |
| [03b](03b-licence-and-signing-checks.md) · [#4](https://github.com/AndreaAzzarello/kalei/issues/4) | Licence and signing checks: Inno Setup, SignPath, logotype font | – | S | none | ready-for-agent | – |
| [04](04-in-app-donut-spike.md) · [#5](https://github.com/AndreaAzzarello/kalei/issues/5) | In-app Donut spike: drag over an Avalonia window, latency, two monitors | – | M | S3, S4 | needs-owner-hardware | – |

## Core and first vertical flow
| # | Title | Blocked by | Size | Rules | Status | Manual check |
|---|---|---|---|---|---|---|
| [05](05-solution-skeleton.md) · [#6](https://github.com/AndreaAzzarello/kalei/issues/6) | Solution skeleton: projects, adapter interfaces, paths adapter, quality gates | 03a | M | S3, S6, S10 | ready-for-agent | – |
| [06](06-header-detection-images.md) · [#7](https://github.com/AndreaAzzarello/kalei/issues/7) | Format detection from the header for images | 05 | S | S3 | ready-for-agent | – |
| [07a](07a-ipc-protocol-and-worker-host.md) · [#8](https://github.com/AndreaAzzarello/kalei/issues/8) | IPC protocol and Worker host: messages, watchdog decisions, fake launcher | 05 | M | S1, S2, S3 | ready-for-agent | – |
| [08](08-broker-and-output-writing.md) · [#10](https://github.com/AndreaAzzarello/kalei/issues/10) | Broker and Output file writing: safe temporary file, journal, naming | 05 | M | S4, S7 | ready-for-agent | – |
| [07b](07b-windows-sandbox-launcher.md) · [#9](https://github.com/AndreaAzzarello/kalei/issues/9) | Windows sandbox launcher: Job Object, AppContainer, first-run set-up | 01, 05, 07a, 08 | L | S1, S2, S5, S7 | ready-for-agent | yes |
| [07c](07c-network-canary.md) · [#50](https://github.com/AndreaAzzarello/kalei/issues/50) | Network canary: prove at start-up that the sandbox blocks the network | 07a, 07b, 10a, 26 | M | S5, S10 | ready-for-agent | yes |
| [09a](09a-images-module-core.md) · [#11](https://github.com/AndreaAzzarello/kalei/issues/11) | Images Module core: Worker host, manifest, PNG, JPG and WebP from Core to file | 02, 06, 07a, 07b, 08 | L | S2, S3, S5, S8 | ready-for-agent | – |
| [09b](09b-images-module-behaviours.md) · [#12](https://github.com/AndreaAzzarello/kalei/issues/12) | Images Module behaviours: GIF as source, TIFF, options, metadata, flattening | 09a | M | S2, S3 | ready-for-agent | – |
| [10a](10a-main-window-file-card.md) · [#13](https://github.com/AndreaAzzarello/kalei/issues/13) | Main window: Drop zone, file card, target list and Category accent | 09a | L | S3, S6, S10 | ready-for-agent | yes |
| [10b](10b-main-window-convert.md) · [#14](https://github.com/AndreaAzzarello/kalei/issues/14) | Main window: Convert, progress, Cancel, Done and Error | 10a | M | S3, S4, S10 | ready-for-agent | – |
| [11](11-donut-library.md) · [#15](https://github.com/AndreaAzzarello/kalei/issues/15) | Donut library: geometry, hit-testing, Format intersection | 05 | M | none | ready-for-agent | – |
| [12a](12a-donut-control.md) · [#16](https://github.com/AndreaAzzarello/kalei/issues/16) | Donut control: drawing, hover and Reduce animations | 10a, 11 | M | S3 | ready-for-agent | yes |
| [12b](12b-in-app-donut-host.md) · [#17](https://github.com/AndreaAzzarello/kalei/issues/17) | In-app Donut host: drag over the window, release on a Slice | 04, 07a, 10b, 12a | L | S3, S4 | ready-for-agent | yes |

## Rest of 1.0
| # | Title | Blocked by | Size | Rules | Status | Manual check |
|---|---|---|---|---|---|---|
| [03c](03c-w7-cold-start-breakdown.md) · [#56](https://github.com/AndreaAzzarello/kalei/issues/56) | W7 cold start: where the time goes | – | S | none | needs-owner-hardware | – |
| [13](13-probe-and-preview.md) · [#18](https://github.com/AndreaAzzarello/kalei/issues/18) | Probe and Preview in the Worker: safe previews and thumbnails | 07a, 10a | M | S2, S3 | ready-for-agent | – |
| [15](15-name-clash-and-output-folder.md) · [#21](https://github.com/AndreaAzzarello/kalei/issues/21) | Name clash choice and Output folder choice | 08, 10b | S | S4 | ready-for-agent | – |
| [14a](14a-batch-engine.md) · [#19](https://github.com/AndreaAzzarello/kalei/issues/19) | Batch engine: folder walk, skip reasons, scheduling | 07b, 08, 15 | M | S2, S4 | ready-for-agent | – |
| [14b](14b-batch-window.md) · [#20](https://github.com/AndreaAzzarello/kalei/issues/20) | Batch window: list, status, Cancel all, Retry failed, mixed files | 14a, 10b, 12b | M | S2, S4 | ready-for-agent | – |
| [16](16-images-avif-gif.md) · [#22](https://github.com/AndreaAzzarello/kalei/issues/22) | Images: AVIF, and GIF as a target | 02, 09b | S | S2 | ready-for-agent | – |
| [17](17-images-bmp-ico.md) · [#23](https://github.com/AndreaAzzarello/kalei/issues/23) | Images: BMP and ICO codecs | 02, 09b | M | S2, S3 | ready-for-agent | – |
| [18](18-images-heic-via-wic.md) · [#24](https://github.com/AndreaAzzarello/kalei/issues/24) | Images: HEIC reading through WIC | 02, 09a | M | S3, S5 | ready-for-agent | – |
| [19](19-text-like-detection.md) · [#25](https://github.com/AndreaAzzarello/kalei/issues/25) | Detection of text-like Formats in the Probe | 06, 07a | M | S3, S9 | ready-for-agent | – |
| [20a](20a-data-csv-tsv.md) · [#26](https://github.com/AndreaAzzarello/kalei/issues/26) | Data: CSV and TSV | 07b, 19, 09a | M | S2, S9 | ready-for-agent | – |
| [20b](20b-data-xlsx.md) · [#27](https://github.com/AndreaAzzarello/kalei/issues/27) | Data: XLSX with CSV and TSV | 20a | M | S2, S9 | ready-for-agent | yes |
| [21](21-data-tree.md) · [#28](https://github.com/AndreaAzzarello/kalei/issues/28) | Data: JSON, YAML and XML | 07b, 19, 09a | M | S2, S9 | ready-for-agent | – |
| [22](22-json-tabular.md) · [#29](https://github.com/AndreaAzzarello/kalei/issues/29) | Data: JSON to and from tables | 20b, 21 | M | S9 | ready-for-agent | – |
| [23](23-text-module.md) · [#30](https://github.com/AndreaAzzarello/kalei/issues/30) | Text: TXT, Markdown and HTML | 07b, 19, 09a | M | S9 | ready-for-agent | – |
| [24](24-settings-and-persistence.md) · [#31](https://github.com/AndreaAzzarello/kalei/issues/31) | Settings and persistence | 10b | M | S6, S10 | ready-for-agent | – |
| [26](26-logging-policy.md) · [#33](https://github.com/AndreaAzzarello/kalei/issues/33) | Logging: format, rotation and the no-paths test | 05, 09a | S | S6, S10 | ready-for-agent | – |
| [27](27-browser-drops-and-origin-mark.md) · [#34](https://github.com/AndreaAzzarello/kalei/issues/34) | Drops from a browser and the origin mark | 08, 10b | M | S7, S12 | ready-for-agent | yes |
| [28](28-limits-page.md) · [#35](https://github.com/AndreaAzzarello/kalei/issues/35) | Settings: Limits page | 14a, 24 | S | S2 | ready-for-agent | – |
| [25](25-localisation-and-error-catalogue.md) · [#32](https://github.com/AndreaAzzarello/kalei/issues/32) | Localisation completeness and the message catalogue | 10b, 14b, 15, 24, 27, 28 | M | S10 | ready-for-agent | – |
| [37](37-advanced-panel.md) · [#44](https://github.com/AndreaAzzarello/kalei/issues/44) | Advanced panel: options for the pair, reset, CSV overrides | 09b, 10b, 20a, 22 | M | S9 | ready-for-agent | – |
| [29](29-accessibility-pass.md) · [#36](https://github.com/AndreaAzzarello/kalei/issues/36) | Accessibility pass | 12b, 14b, 15, 24, 28, 37 | M | none | ready-for-agent | yes |

## Release
| # | Title | Blocked by | Size | Rules | Status | Manual check |
|---|---|---|---|---|---|---|
| [30](30-portable-zip-sums-notices.md) · [#37](https://github.com/AndreaAzzarello/kalei/issues/37) | Portable ZIP, provisional SHA256SUMS, third-party notices and About | 03a, 10a, 39 | M | S6 | ready-for-agent | – |
| [31](31-per-user-installer.md) · [#38](https://github.com/AndreaAzzarello/kalei/issues/38) | Per-user installer, clean uninstall, final size and antivirus checks | 01, 03a, 03b, 07b, 30 | M | S1, S5 | needs-owner-hardware | – |
| [32](32-code-signing-and-final-sums.md) · [#39](https://github.com/AndreaAzzarello/kalei/issues/39) | Code signing, only if confirmed, and the final SHA256SUMS | 31, 03b | S | none | needs-owner-hardware | – |
| [33](33-final-verification.md) · [#40](https://github.com/AndreaAzzarello/kalei/issues/40) | Final verification: the S1–S12 gate and a release dry run | 07c, 13, 14b, 16, 17, 18, 22, 23, 25, 26, 27, 29, 32, 37 | L | S1–S12 | needs-owner-hardware | – |
| [38](38-native-aot-trial.md) · [#51](https://github.com/AndreaAzzarello/kalei/issues/51) | Native AOT trial on a GitHub Actions Windows runner | 05, 30, 39 | S | none | ready-for-agent | – |
| [39](39-ci-github-actions.md) · [#52](https://github.com/AndreaAzzarello/kalei/issues/52) | CI on GitHub Actions: build, test, format and trimming checks on every push and PR | 05 | S | S6, S10 | ready-for-agent | – |

## Not blocking 1.0
| # | Title | Blocked by | Size | Rules | Status | Manual check |
|---|---|---|---|---|---|---|
| [34](34-global-gesture-spike.md) · [#41](https://github.com/AndreaAzzarello/kalei/issues/41) | Spike (1.2): global gesture, Sentinel, wheel, false triggers | 12b | L | S11 | needs-owner-hardware | – |
| [35](35-linux-spike.md) · [#42](https://github.com/AndreaAzzarello/kalei/issues/42) | Spike: Linux sandbox, In-app Donut and packaging (L1–L4, L6) | – | L | S1, S5 | needs-owner-hardware | – |
| [36](36-macos-spike.md) · [#43](https://github.com/AndreaAzzarello/kalei/issues/43) | Spike: macOS sandbox, Gatekeeper and In-app Donut (M1–M3, M5, M6) | – | L | S1, S5 | needs-owner-hardware | – |

## Traceability: user story to tickets
Every one of the 65 [user stories](../spec/1.0-windows.md#user-stories) is delivered by at least one ticket.

| Story | Tickets |
|---|---|
| 1 | [10a](10a-main-window-file-card.md) |
| 2 | [10a](10a-main-window-file-card.md) |
| 3 | [06](06-header-detection-images.md), [10a](10a-main-window-file-card.md), [19](19-text-like-detection.md) |
| 4 | [06](06-header-detection-images.md), [10a](10a-main-window-file-card.md), [19](19-text-like-detection.md) |
| 5 | [10a](10a-main-window-file-card.md) |
| 6 | [10a](10a-main-window-file-card.md) |
| 7 | [10a](10a-main-window-file-card.md) |
| 8 | [07b](07b-windows-sandbox-launcher.md), [10b](10b-main-window-convert.md) |
| 9 | [10b](10b-main-window-convert.md) |
| 10 | [10b](10b-main-window-convert.md) |
| 11 | [08](08-broker-and-output-writing.md), [10b](10b-main-window-convert.md) |
| 12 | [12b](12b-in-app-donut-host.md) |
| 13 | [12a](12a-donut-control.md) |
| 14 | [12b](12b-in-app-donut-host.md) |
| 15 | [12b](12b-in-app-donut-host.md) |
| 16 | [12b](12b-in-app-donut-host.md) |
| 17 | [12b](12b-in-app-donut-host.md), [29](29-accessibility-pass.md) |
| 18 | [12a](12a-donut-control.md), [29](29-accessibility-pass.md) |
| 19 | [12b](12b-in-app-donut-host.md) |
| 20 | [11](11-donut-library.md) |
| 21 | [14b](14b-batch-window.md) |
| 22 | [14a](14a-batch-engine.md) |
| 23 | [14b](14b-batch-window.md) |
| 24 | [14b](14b-batch-window.md) |
| 25 | [14b](14b-batch-window.md) |
| 26 | [14a](14a-batch-engine.md) |
| 27 | [14a](14a-batch-engine.md) |
| 28 | [14b](14b-batch-window.md) |
| 29 | [14b](14b-batch-window.md) |
| 30 | [37](37-advanced-panel.md) |
| 31 | [37](37-advanced-panel.md) |
| 32 | [09b](09b-images-module-behaviours.md) |
| 33 | [09b](09b-images-module-behaviours.md) |
| 34 | [09b](09b-images-module-behaviours.md), [37](37-advanced-panel.md) |
| 35 | [15](15-name-clash-and-output-folder.md), [24](24-settings-and-persistence.md) |
| 36 | [08](08-broker-and-output-writing.md), [15](15-name-clash-and-output-folder.md) |
| 37 | [15](15-name-clash-and-output-folder.md) |
| 38 | [20a](20a-data-csv-tsv.md), [37](37-advanced-panel.md) |
| 39 | [22](22-json-tabular.md) |
| 40 | [10b](10b-main-window-convert.md), [25](25-localisation-and-error-catalogue.md) |
| 41 | [08](08-broker-and-output-writing.md), [07b](07b-windows-sandbox-launcher.md), [09a](09a-images-module-core.md), [20a](20a-data-csv-tsv.md), [20b](20b-data-xlsx.md), [21](21-data-tree.md) |
| 42 | [10b](10b-main-window-convert.md), [28](28-limits-page.md) |
| 43 | [07b](07b-windows-sandbox-launcher.md), [09a](09a-images-module-core.md), [33](33-final-verification.md) |
| 44 | [20a](20a-data-csv-tsv.md), [20b](20b-data-xlsx.md) |
| 45 | [23](23-text-module.md) |
| 46 | [26](26-logging-policy.md) |
| 47 | [18](18-images-heic-via-wic.md) |
| 48 | [13](13-probe-and-preview.md) |
| 49 | [13](13-probe-and-preview.md) |
| 50 | [27](27-browser-drops-and-origin-mark.md) |
| 51 | [27](27-browser-drops-and-origin-mark.md) |
| 52 | [27](27-browser-drops-and-origin-mark.md) |
| 53 | [24](24-settings-and-persistence.md), [25](25-localisation-and-error-catalogue.md) |
| 54 | [10a](10a-main-window-file-card.md), [24](24-settings-and-persistence.md) |
| 55 | [24](24-settings-and-persistence.md) |
| 56 | [28](28-limits-page.md) |
| 57 | [24](24-settings-and-persistence.md) |
| 58 | [29](29-accessibility-pass.md) |
| 59 | [29](29-accessibility-pass.md) |
| 60 | [10a](10a-main-window-file-card.md), [29](29-accessibility-pass.md) |
| 61 | [31](31-per-user-installer.md) |
| 62 | [30](30-portable-zip-sums-notices.md) |
| 63 | [30](30-portable-zip-sums-notices.md) |
| 64 | [30](30-portable-zip-sums-notices.md) |
| 65 | [31](31-per-user-installer.md) |
