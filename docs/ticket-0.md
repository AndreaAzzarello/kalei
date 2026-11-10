# Ticket 0 — Feasibility spike

Ticket 0 proves, with measurements, the claims that the design cannot settle on paper. It writes **no product code**: spike code is throwaway, kept on a branch, and only the measured results and decisions return to the documents. Each test has a number to hit; below the "fail" line the stated fallback applies and the named ADR or requirement is changed. Nothing here is solved in words: a row stays open until it has a measured result recorded in `HISTORY.md` and the ADR it unblocks.

**Reference machine**: the lowest-specification Windows PC the owner can test on; its CPU, RAM, disk and Windows build are written in the report. Timings: median of 10 runs, cold (after a reboot or with the file cache dropped for the first run). Sizes are measured on the final published artifact.

## Part A — Windows (blocks 1.0)

| ID | Test | Measure | Pass | Fail → fallback | Unblocks |
|---|---|---|---|---|---|
| W1 | AppContainer + .NET runtime + libvips worker converts PNG → AVIF from inherited handles | conversion succeeds; Worker has no file path access | succeeds on Windows 11 **and** Windows 10 22H2 | cannot run in AppContainer → **S5 fails: "no network" promise removed from PRD/README**; Job Object + restricted token kept as defence in depth only | ADR 0003, S5 |
| W2 | Network from inside the Worker | TCP connect to a reachable host and a DNS lookup, both attempted | both fail | any succeeds → as W1 | S5 |
| W3 | Handles and seekability | TIFF and AVIF read through inherited handles; a seek-heavy TIFF | works; handle must be seekable | pipe-only input → copy not allowed (S7), so that format is dropped from 1.0 | ADR 0003, ipc-spec |
| W4 | Worker reads its own files | AppContainer can load Worker, .NET runtime and libvips from the install folder after the installer sets the ACL (read+execute for ALL APPLICATION PACKAGES) | loads with per-user install, no admin | needs admin or another folder → installer redesign | installer |
| W5 | WIC / HEIC inside AppContainer | HEIC decode with the user's WIC extensions; list of extensions actually required | decode works, or a clear message when missing | WIC unusable in sandbox → HEIC not in 1.0 | matrix |
| W6 | libvips content (NetVips.Native) | AVIF write, GIF write (cgif), BMP and ICO codecs, loader list, SVG loader can be disabled, `vips_block_untrusted_set` works | AVIF, GIF write present; untrusted loaders blockable | missing format → removed from the 1.0 matrix or own codec added (BMP, ICO: own decoder/encoder with fuzzing) | ADR 0006, matrix |
| W7 | Native AOT with Avalonia + Skia | `dotnet publish` with AOT succeeds; cold start of the UI | publish OK **and** start ≤ 1 s | fails or slower → ReadyToRun + trimming, ADR 0001 updated | ADR 0001 |
| W8 | Installer size | size of per-user installer and portable ZIP with the 1.0 Modules | ≤ 45 MB target; ≤ 60 MB acceptable | > 60 MB → scope or libraries reviewed before 1.0 | PRD |
| W9 | Trimmed Worker | each library marked n.v. in the TRD publishes trimmed with warnings as errors | all pass | failing library replaced (e.g. CSV: Sep → CsvHelper) | TRD |
| W10 | In-app Donut: drag over an Avalonia window | OLE drag-over opens an overlay; time from first drag event to visible Donut; whether format information is available before the drop | ≤ 100 ms to overlay; Format known before drop, or "analysing" state works | file list only at drop → Donut offers formats from extension as a hint, marked "analysing" | ADR 0008 |
| W11 | Wheel during a drag | mouse-wheel events reach the Donut (in-app) and the hook (global) during an OLE drag | events arrive | not delivered → Face flip by Tab only in-app; global Donut has one Face | ADR 0008/0009 |
| W12 | Minimal mouse hook + non-activating overlay | gesture → overlay latency, cold start | ≤ 300 ms | slower → pre-started lightweight overlay; else 1.2 reduced | ADR 0009 |
| W13 | Sentinel resident memory | private working set at rest, after 10 minutes | ≤ 20 MB | > 20 MB → look for what holds memory; if not fixable, residency stays off by default and the figure is published | ADR 0009 |
| W14 | False triggers | 50 text selections, 50 window drags and 50 file-manager drags with the modifier held | Donut opens on file drags only; 0 opens in the other 100 | opens on others → stricter gesture (longer threshold, file-drag detection), else the global Donut is cut | ADR 0009 |
| W15 | Elevated windows and hook timeouts | drag onto/from an elevated window; callback deadline when the system is loaded | behaviour documented; hook not removed by the system in a 10-minute stress run | removed → hook callback only posts to its own thread | ADR 0009 |
| W16 | Watchdog and cancel | a hung Worker is killed within 5 s of missing heartbeat; cancel leaves no partial file; crash leaves none after the next start (journal) | all three | partial files remain → journal logic fixed before 1.0 | S1, S7 |
| W17 | Antivirus behaviour | Defender and one more consumer AV on the Sentinel and the installer (SmartScreen, heuristics) | no detection, or documented | detections → code signing earlier than 1.2; document | PRD |
| W18 | Installer licence and signing | Inno Setup licence text for the used version read; SignPath Foundation eligibility terms read | both documented | unsuitable → another installer; signing path chosen before 1.0 | TRD |

## Part B — Linux spike (does not block 1.0)
Goal: decide whether Linux can ship in 1.x and with which promise. Distros: Ubuntu 24.04, Fedora (current), Debian stable.

| ID | Test | Pass | Fail → fallback |
|---|---|---|---|
| L1 | Worker with `no_new_privs` + seccomp (`socket()` only `AF_UNIX`) + Landlock, no privileges | TCP connect, UDP send and DNS lookup all **fail**; opening any path outside inherited descriptors fails; conversion of PNG → WebP works | seccomp fails → no Linux build; Landlock missing (kernel < 5.13) → run with seccomp only and say so; no network block → "no network" promise not made on Linux |
| L2 | Libraries load before Landlock is applied; .NET runtime and libvips work under the restrictions | works | document needed start-up order; otherwise no Linux build |
| L3 | bubblewrap `--unshare-all` as an extra layer: system `bwrap`, inside Flatpak (`flatpak-spawn --sandbox --no-network --forward-fd`), and from an AppImage on Ubuntu 24.04 | works where user namespaces are allowed; failure is detected and falls back to L1 | none needed (L1 is the base) |
| L4 | In-app Donut under X11 and Wayland; drag from the file manager gives usable file list | overlay opens on both | Wayland without it → Donut disabled there, drop zone remains |
| L5 | Global hook on X11; Wayland check | works on X11; clearly unavailable on Wayland | feature off, documented |
| L6 | Packaging: Flatpak build and AppImage run on a clean VM; size; `libfuse2t64` need | both start | choose the one that works; document the other |

## Part C — macOS spike (does not block 1.0)
Goal: decide whether macOS can ship in 1.x without depending on an Apple account.

| ID | Test | Pass | Fail → fallback |
|---|---|---|---|
| M1 | Worker signed **ad hoc** with `app-sandbox` + `inherit`, no `network.client`, started with `posix_spawn` | TCP connect and DNS lookup both **fail**; the Worker reads and writes only the inherited descriptors; PNG → WebP works | network not blocked → "no network" promise not made on macOS; descriptors unusable → XPC with `NSFileHandle` tested; neither works → no macOS build |
| M2 | ImageIO and HEIC inside the sandbox | decode works | HEIC not offered on macOS |
| M3 | Gatekeeper with the ad-hoc build on the current macOS and Apple Silicon | "Open Anyway" path works as documented | README instructions corrected |
| M4 | `CGEventTap` with Input Monitoring; behaviour of the permission after an ad-hoc rebuild | works; re-grant need documented | global Donut off on macOS |
| M5 | Drag from Finder into the in-app Donut | works | drop zone only |
| M6 | (Only if an Apple account is active) Developer ID signing and notarisation of the same build | works, and the sandbox result does not change | none: notarisation is only distribution |

## Order of the tickets after Ticket 0
Ticket 1 (solution skeleton, Core, Platform adapters for Windows, format detection, Broker, IPC, image Module, in-app Donut) needs W1–W7 and W10; the data and text Modules need W9; the installer needs W4, W8, W17, W18; the global Donut (1.2) needs W11–W15; Linux and macOS tickets need Parts B and C.
