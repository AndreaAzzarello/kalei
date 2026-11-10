# Ticket 0 — Feasibility spike

Ticket 0 proves, with measurements, the claims that the design cannot settle on paper. It writes **no product code**: spike code is throwaway, kept on a branch, and only the measured results and decisions return to the documents. Each test has a number to hit; below the "fail" line the stated fallback applies and the named ADR or requirement is changed. Nothing here is solved in words: a row stays open until it has a measured result recorded in `HISTORY.md` and the ADR it unblocks.

**Names**: "Ticket 0" in the other documents means this document and the spike tickets that carry out its proofs: 01–04 (Windows), 34 (global gesture), 35 (Linux) and 36 (macOS); the tickets themselves are numbered from 01 ([tickets](tickets/README.md)).

## Reference machines
- **Main (worst case, reproducible)**: Hyper-V virtual machine, Windows 10 22H2, 2 vCPU, 4 GB RAM, virtual SSD. The thresholds for start-up, memory and Donut latency are measured here.
- **Windows 11**: the owner's desktop PC with two monitors; also used to try the Donut across screens and with different DPI.
- **macOS**: the owner's Mac mini (Apple M1, 8 GB RAM), for M1–M6.
- **Linux**: virtual machines with Ubuntu 24.04 and the current Fedora, for L1–L6.

Timings are the median of 10 runs, cold (after a reboot, or with the file cache dropped for the first run); the machine, Windows build and run count go in the report. Sizes are measured on the final published artifact.

## Part A — Windows (blocks 1.0)

| ID | Test | Measure | Pass | Fail → fallback | Unblocks |
|---|---|---|---|---|---|
| W1 | AppContainer + .NET runtime + libvips worker converts PNG → AVIF from inherited handles | conversion succeeds; Worker has no file path access | succeeds on Windows 11 **and** Windows 10 22H2 | cannot run in AppContainer → **S5 fails: "no network" promise removed from PRD/README**; Job Object + restricted token kept as defence in depth only | ADR 0003, S5 |
| W2 | Network from inside the Worker | TCP connect to a reachable host and a DNS lookup, both attempted | both fail | any succeeds → as W1 | S5 |
| W3 | Handles and seekability | TIFF and AVIF read through inherited handles; a seek-heavy TIFF | works; handle must be seekable | pipe-only input → copy not allowed (ADR 0003; the only copies are the declared spooled browser files, S12), so that format is dropped from 1.0 | ADR 0003, ipc-spec |
| W4 | Worker reads its own files | AppContainer can load Worker, .NET runtime and libvips from the install folder after the installer sets the ACL (read+execute for ALL APPLICATION PACKAGES) | loads with per-user install, no admin | needs admin or another folder → installer redesign | installer |
| W5 | WIC / HEIC inside AppContainer | HEIC decode with the user's WIC extensions; list of extensions actually required | decode works, or a clear message when missing | WIC unusable in sandbox → HEIC not in 1.0 | matrix |
| W6 | libvips content (NetVips.Native) | AVIF write, GIF write (cgif), BMP and ICO codecs, loader list, SVG loader can be disabled, `vips_block_untrusted_set` works | AVIF, GIF write present; untrusted loaders blockable | missing format → removed from the 1.0 matrix or own codec added (BMP, ICO: own decoder/encoder with fuzzing) | ADR 0006, matrix |
| W7 | Native AOT with Avalonia + Skia | `dotnet publish` with AOT succeeds; cold start of the UI, from process start to the first frame drawn with the window accepting input | publish OK **and** start ≤ 1 s | fails or slower → ReadyToRun + trimming, ADR 0001 updated | ADR 0001 |
| W8 | Installer size | size of per-user installer and portable ZIP with the 1.0 Modules (first estimate from a prototype in ticket 03a, final measure on the real installer in ticket 31) | ≤ 45 MB target; ≤ 60 MB acceptable | > 60 MB → scope or libraries reviewed before 1.0 | PRD |
| W9 | Trimmed Worker | each library marked n.v. (not verified) in the TRD publishes trimmed with warnings as errors | all pass | failing library replaced (e.g. CSV: Sep → CsvHelper) | TRD |
| W10 | In-app Donut: drag over an Avalonia window | OLE drag-over opens an overlay; time from the first drag-enter event received by the window to the first frame of the overlay; whether format information is available before the drop | ≤ 100 ms to overlay; Format known before drop, or "analysing" state works | file list only at drop → Donut offers formats from extension as a hint, marked "analysing" | ADR 0008 |
| W16 | Watchdog and cancel (run in tickets 07b and 08) | a hung Worker is killed within 10 s of missing heartbeat; cancel leaves no partial file; crash leaves none after the next start (journal) | all three | partial files remain → journal logic fixed before 1.0 | S1, S7 |
| W17 | Antivirus behaviour | Microsoft Defender and one other free consumer antivirus (named in the report) on the installer and the app (SmartScreen, heuristics); run on the final installer in ticket 31 (the Sentinel is checked in ticket 34) | no detection, or documented | detections → code signing earlier than 1.2; document | PRD |
| W18 | Installer licence and signing | Inno Setup licence text for the used version read; SignPath Foundation eligibility terms read | both documented | unsuitable → another installer; signing path chosen before 1.0 | TRD |

## Part A2 — Windows global-gesture spike (for 1.2, does not block 1.0)
Same table layout as Part A. Run in ticket 34. They matter only for the Global Donut (1.2) and for the back Face of the Donut, which 1.0 does not have (at most 8 targets). W11 moved here from Part A for that reason; W11g is its global counterpart.

| ID | Test | Measure | Pass | Fail → fallback | Unblocks |
|---|---|---|---|---|---|
| W11 | Wheel during an in-app drag | mouse-wheel events reach the in-app Donut during an OLE drag | events arrive | not delivered → Face flip by Tab only in-app | ADR 0008 |
| W11g | Wheel events reach the hook during a drag | wheel events seen by the minimal hook while a file drag is in progress | events arrive | not delivered → the global Donut has one Face | ADR 0009 |
| W12 | Minimal mouse hook + non-activating overlay | gesture → overlay latency, cold start | ≤ 300 ms | slower → pre-started lightweight overlay; else 1.2 reduced | ADR 0009 |
| W13 | Sentinel resident memory | private working set at rest, after 10 minutes | ≤ 20 MB | > 20 MB → look for what holds memory; if not fixable, residency stays off by default and the figure is published | ADR 0009 |
| W14 | False triggers | 50 text selections, 50 window drags and 50 file-manager drags with the modifier held | Donut opens on file drags only; 0 opens in the other 100 | opens on others → stricter gesture (longer threshold, file-drag detection), else the global Donut is cut | ADR 0009 |
| W15 | Elevated windows and hook timeouts | drag onto/from an elevated window; callback deadline when the system is loaded | behaviour documented; hook not removed by the system in a 10-minute stress run | removed → hook callback only posts to its own thread | ADR 0009 |

## Part B — Linux spike (does not block 1.0)
Goal: decide whether Linux can ship in 1.x and with which promise. Distros: Ubuntu 24.04 and Fedora (current), as virtual machines.

| ID | Test | Pass | Fail → fallback |
|---|---|---|---|
| L1 | Worker with `no_new_privs` + seccomp (`socket()` only `AF_UNIX`) + Landlock, no privileges | TCP connect, UDP send and DNS lookup all **fail**; opening any path outside inherited descriptors fails; conversion of PNG → WebP works | seccomp fails → no Linux build; Landlock missing (kernel < 5.13) → run with seccomp only and say so; no network block → "no network" promise not made on Linux |
| L2 | Libraries load before Landlock is applied; .NET runtime and libvips work under the restrictions | works | document needed start-up order; otherwise no Linux build |
| L3 | bubblewrap `--unshare-all` as an extra layer: system `bwrap`, inside Flatpak (`flatpak-spawn --sandbox --no-network --forward-fd`), and from an AppImage on Ubuntu 24.04 | works where user namespaces are allowed; failure is detected and falls back to L1 | none needed (L1 is the base) |
| L4 | In-app Donut under X11 and Wayland; drag from the file manager gives usable file list | overlay opens on both | Wayland without it → Donut disabled there, drop zone remains |
| L5 | Global hook on X11; Wayland check (run in ticket 34, 1.2) | works on X11; clearly unavailable on Wayland | feature off, documented |
| L6 | Packaging: Flatpak build and AppImage run on a clean VM; size; `libfuse2t64` need | both start | choose the one that works; document the other |

## Part C — macOS spike (does not block 1.0)
Goal: decide whether macOS can ship in 1.x without depending on an Apple account.

| ID | Test | Pass | Fail → fallback |
|---|---|---|---|
| M1 | Worker signed **ad hoc** with `app-sandbox` + `inherit`, no `network.client`, started with `posix_spawn` | TCP connect and DNS lookup both **fail**; the Worker reads and writes only the inherited descriptors; PNG → WebP works | network not blocked → "no network" promise not made on macOS; descriptors unusable → XPC with `NSFileHandle` tested; neither works → no macOS build |
| M2 | ImageIO and HEIC inside the sandbox | decode works | HEIC not offered on macOS |
| M3 | Gatekeeper with the ad-hoc build on the current macOS and Apple Silicon | "Open Anyway" path works as documented | README instructions corrected |
| M4 | `CGEventTap` with Input Monitoring; behaviour of the permission after an ad-hoc rebuild (run in ticket 34, 1.2) | works; re-grant need documented | global Donut off on macOS |
| M5 | Drag from Finder into the in-app Donut | works | drop zone only |
| M6 | (Only if an Apple account is active) Developer ID signing and notarisation of the same build | works, and the sandbox result does not change | none: notarisation is only distribution |

## Which ticket runs which proof
The tickets are in [tickets/README.md](tickets/README.md).

| Proofs | Ticket |
|---|---|
| W1, W2, W3, W4 | 01 Sandbox spike |
| W5, W6 | 02 Image engine content |
| W7, W9, and a first W8 estimate | 03a Build spike |
| W18 | 03b Licence and signing checks |
| W10 | 04 In-app Donut spike |
| W16 | 07b (hang and cancel) and 08 (journal) |
| W8 (final), W17 | 31 Installer |
| W11, W11g, W12, W13, W14, W15, L5, M4 | 34 Global gesture spike (1.2) |
| L1–L4, L6 | 35 Linux spike |
| M1–M3, M5, M6 | 36 macOS spike |

## Results
Measured or read results, newest first. A row not listed here is still open.

### W18 — Installer licence and signing (ticket 03b, read 2026-11-10; no machine needed)
- **Inno Setup 7.1.1**: licence read; use for any purpose is permitted, free; the site only asks commercial users to buy a licence. Pass. Owner decision: the installer counts as binary redistribution, so the Inno Setup copyright and web address go into THIRD-PARTY-NOTICES (ticket 30). Details and links: [TRD](TRD.md).
- **SignPath Foundation**: terms read; requires an existing public release, MFA for all team members, manual approval of every release, a "Code signing policy" on the home page; the Foundation is the publisher and may refuse. Result: **needs the owner to apply**, after a first public release; the owner will apply then, and 1.0 may ship unsigned (requirements recorded in ticket 32). The `/apply` form and the supported CI are **not verified**.
- **Mayenne Sans**: original `OFL.txt` **not found**, and **not needed**: only Bricolage Grotesque (OFL 1.1, declared by its authors) is cited, with no mention of the ancestor font.

Dependencies after the spikes: the solution skeleton needs 03a; the sandbox launcher needs 01; the image Module needs 02; the in-app Donut host needs 04; the installer needs 01, 03a and 03b.
