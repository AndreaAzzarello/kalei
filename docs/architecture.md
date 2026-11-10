# Kalei — Architecture

Decisions behind this are in [docs/adr](adr/). Terms: [GLOSSARY.md](../GLOSSARY.md). Release numbers: [PRD](PRD.md#release-order-the-single-source-for-version-numbers-other-documents-point-here). Proofs still open: [ticket-0.md](ticket-0.md).

## Shape

```
Kalei.App (Avalonia, Native AOT)        UI only: states, drop zone, accent, settings
   │ calls
Kalei.Core (pure .NET, no UI)           format detection (header only), queue, limits, module manager,
   │                                    output naming, temp/log policy, Broker
   │ versioned IPC on inherited pipes + handles (ipc-spec.md)
Kalei.Worker.* (one process per Conversion, Probe or Preview)   sandboxed; hosts one or more Modules' Engines
   ├─ Kalei.Worker.Builtin  (built-in Modules: images, data, text)
   └─ <optional module workers> (from 1.1)
```

```
Kalei.Sentinel (1.2 only, opt-in)       mouse hook + overlay launcher, ≤ 20 MB at rest
   │ starts on gesture
Kalei.App overlay host (global Donut)   same Donut component as the in-app host
```

The Core contains no operating-system API. Everything that depends on the OS sits behind a **Platform adapter** (interfaces listed in [ADR 0010](adr/0010-multiplatform-architecture-windows-first.md)); the per-OS implementations are summarised in the capability table at the end of this document.

## Donut component (ADR 0008)
`Kalei.Donut` is a UI-independent library: slice layout, angles, hit-testing and the choice of Formats that suit a mix of files, all unit-testable. A back Face and its flipping are not part of 1.0 (at most 8 targets exist); they come with the first Module that has more. A thin Avalonia control draws it. Two hosts: the **in-app host** (1.0) shows it as an overlay inside the main window during an operating-system drag-over; the **global host** (1.2) shows it in a transparent, topmost, non-activating window. The Donut asks the Core which target Formats suit the dragged files and returns the chosen Format to the Core's queue, which starts the Conversion as usual. A drop is always reported to the file manager as *Copy*, so a source is never removed. Whether the Format of the files is known at the first drag event (before the drop) is a Ticket 0 check; until it is known the Donut shows an "analysing" state, and detection itself follows S3.

## Sentinel and hook (ADR 0009, 1.2)
`Kalei.Sentinel` is a minimal separate process: a mouse hook and the state of the configured modifier key while a drag is in progress (S11). No keyboard hook, no keystroke recording, no network, nothing logged. On the gesture it starts the overlay host and hands it the cursor position. Residency and start at sign-in are opt-in. Open proofs (Ticket 0): cold-start latency, RAM at rest, wheel events during a drag, false triggers, elevated windows, antivirus behaviour. The overlay host is started only after the Sentinel has checked that the executable is the installed one.

## Core responsibilities
- **Format detection** (S3): the Core reads at most the first 64 KB and applies the signature table in [format-detection.md](format-detection.md); anything needing structure or content (deeper detection, previews, thumbnails) is asked of a Worker through `Probe` and `Preview` messages. Mismatches between content and extension are reported to the user.
- **Queue**: ordered Conversions of a Batch, bounded parallelism (see Limits), cancellation, per-file result.
- **Module manager**: reads each `module.json` ([module-manifest.md](module-manifest.md); formats, Options, version, never Limits), builds the Conversion matrix at runtime. From 1.1 it also installs and uninstalls Optional modules ([ADR 0004](adr/0004-optional-modules-embedded-hashes.md), S8).
- **Broker**: opens the Source file read-only and creates the Output file; passes handles to the Worker; the Worker never sees a path ([ADR 0003](adr/0003-worker-sandbox-and-handle-passing.md)).
- **Output naming**: sanitises names, rejects path traversal and reserved names, resolves clashes to `name (1).ext`, never overwrites.
- **Limits** enforcement before and during Conversion.
- **Conversion graph (after 1.0, not a security rule)**: conversions are modelled as a graph with at most 3 steps per route so multi-step conversions can be offered later; every step would run in its own Worker. In 1.0 every pair is direct and routing is a flat table.

## Conversion lifecycle
1. Core reads the header (S3), detects the Format and checks Limits (size, count, depth).
2. Core picks the Module. If it is an Optional module that is not installed (from 1.1), the user is asked for consent; otherwise the file is skipped with its reason.
3. Broker opens handles; Core starts a Worker through the OS sandbox with a timeout and memory cap (S1, S5).
4. Worker converts over streams and reports progress or result over IPC; the Core's watchdog kills a Worker that stops answering. Cancel is a `Cancel` message: the Worker stops, and the Core kills it if it has not exited after 3 s.
5. The Output is created exclusively under a reserved name `.kalei-<guid>.tmp` in the Output folder, recorded in a journal in Kalei's data folder ([settings spec](spec/settings.md#where-data-lives)), and renamed to its final name without replacing anything (on a clash it retries with `name (n).ext`). On cancel, error or timeout the partial file is removed; after a crash, the journal tells the next start which partial files to delete.
6. The Worker process is always terminated, success or not.

## Security requirements (normative, the only place they are written)
Other documents point here and do not repeat them. They hold for every operating system; how each OS meets them is in the capability table.
- **S1** Each Conversion runs in its own Worker process with timeout, memory cap and cancel, and the Worker is killed when the app exits.
- **S2** Limits on size and count; protection against zip bombs (expansion ratio, total size, nested archives, XLSX included) and malformed or malicious files, with the values in the Limits table.
- **S3** The real type comes from content; the extension is only a hint. The Core reads headers only (at most 64 KB). Every parser of untrusted bytes runs in a Worker: structural detection, previews, thumbnails and the UI never decode untrusted content themselves.
- **S4** Output names and paths are sanitised; no traversal; no silent overwrite; the Source file is never touched.
- **S5 (release gate)** A Worker has no network access, enforced by the operating system's sandbox and not by the Worker's good behaviour. It is tested by a TCP connect and a DNS lookup from inside a Worker, which must both fail. **If this cannot be proved for an OS in Ticket 0, the promise "no network" is removed from PRD and README for that OS, and no weaker mechanism is sold as equivalent.** Weaker mechanisms (for example a restricted token) may remain only as defence in depth and are described as such.
- **S6** No automatic update and no update check. A manual "Open Releases page" button only opens the project's GitHub Releases page in the browser.
- **S7** Temporary files live only in Kalei's own folder, always deleted at the end, on error and on cancel, and stale ones are removed at start-up. Two declared exceptions: the in-flight Output file (`.kalei-<guid>.tmp` in the Output folder, tracked by the journal) and spooled browser files (S12), kept in Kalei's own temporary folder.
- **S8 (manifest rules from 1.0; module integrity from 1.1)** Optional modules are first-party only. Every file of an installed module is hashed and compared with the hashes compiled into the app at install and at every load; Limits always come from the table in the app (with the user's changes from Settings → Limits, within the hard maximum), never from `module.json`; a manifest with a `limits` field or an unknown field is rejected for every Module, built-in ones included, from 1.0. A `.kmod` is verified (SHA-256 of the whole package) before extraction, on a private copy; extraction rejects `..`, absolute paths, links and reparse points, files not listed in the manifest, and sizes or ratios above the cap. Downloads go only to the fixed source's GitHub hosts.
- **S9** XML: DTD and external entities disabled. YAML: no arbitrary type instantiation, limits on aliases and depth. HTML output: sanitised, with a Content-Security-Policy `default-src 'none'; img-src data:; style-src 'unsafe-inline'`, no remote resources. CSV and TSV output: text cells starting with `=`, `+`, `-` or `@` are neutralised (default on, switch in Options); cells that hold a number (for example -5) are written as they are. XLSX: never formulas; text is written as an explicit string cell.
- **S10** Logs contain no paths and no file names; a test feeds sample paths through the app and scans the logs.
- **S11 (from 1.2)** Never a keyboard hook; the global gesture uses a mouse hook plus the state of one chosen modifier key, and nothing typed is recorded ([ADR 0009](adr/0009-mouse-only-hook-and-sentinel.md)).
- **S12** Drops from a browser: only the bytes the browser hands over are accepted; a link without bytes is refused ("Save the file first"); no download code exists except the approved Module install (from 1.1). A virtual file that arrives as a stream is spooled to Kalei's own temporary folder under the same Limits and removed afterwards. The Output keeps the origin mark of the source (the OS flag that marks a file as downloaded; see the capability table) but only that flag, never the source URLs; where the destination cannot hold it (FAT, exFAT, network shares), the Conversion still runs and the user is told.

## Limits
Initial values: tickets 07b, 09a and 14a measure memory, time and Batch size on the main reference machine with generated large files, and this table is changed if the measurements say so. The Core enforces the limits before and during a Conversion; the Worker's own limits come from this table, not from a manifest (S8). The hard maxima are what Settings → Limits allows.

| Limit | Default | Editable in Settings | Hard maximum | Enforced by | Error code |
|---|---|---|---|---|---|
| Source file size, images | 2 GB | yes | 4 GB | Core (before) | `E_SIZE` |
| Source file size, data and text | 200 MB | yes | 1 GB | Core (before) | `E_SIZE` |
| Pixels per image | 250 MP | yes | 500 MP | Worker (header), Core confirms | `E_PIXELS` |
| Files per Batch | 10,000 | yes | 100,000 | Core | `E_COUNT` |
| Folder depth | 32 | yes | 64 | Core | `E_DEPTH` |
| Worker memory | 2 GB | yes | 4 GB | OS sandbox / watchdog | `E_MEM` |
| Timeout per Conversion | 2 min + 1 min per 100 MB, at most 15 min | yes (the 15 min cap) | 30 min | Core watchdog | `E_TIME` |
| Parallel Workers | min(cores ÷ 2, 4, 4 GB ÷ Worker memory) | no | – | Queue | – |
| XLSX / archives | expansion ratio ≤ 100:1, total uncompressed ≤ 1 GB, no nested archives | no | – | Worker, Core confirms | `E_BOMB` |
| YAML | at most 100 aliases, depth 64 | no | – | Worker | `E_COMPLEX` |
| JSON, XML | depth 64 | no | – | Worker | `E_COMPLEX` |

Above the Batch limit the whole Batch is refused with `E_COUNT` and nothing starts. Folder walk: symbolic links and junctions are not followed; hidden and system files are skipped; folders deeper than the depth limit are skipped (`E_DEPTH`, reason "too deep"); each skipped file shows its reason (unsupported format, over a limit, system or hidden, symbolic link or junction, too deep, already in this format, unreadable, clash declined). Result lists of large Batches are virtualised in the UI.

## Capability table per OS
How each requirement is met on each operating system, behind the Platform adapters of [ADR 0010](adr/0010-multiplatform-architecture-windows-first.md). Sources and open proofs for the sandbox rows are in that ADR and in [ticket-0.md](ticket-0.md). Windows cells come from [ADR 0003](adr/0003-worker-sandbox-and-handle-passing.md) and the earlier design; Microsoft's documentation was not re-read in this pass. "To verify" = proof assigned to the spike.

| Element | Windows (1.0) | macOS (1.x) | Linux (1.x) |
|---|---|---|---|
| Worker without network (S5) | AppContainer without the network capability | App Sandbox (`app-sandbox` + `inherit`, no `network.client`); works ad-hoc signed; network block to verify | seccomp (`socket()` only `AF_UNIX`) + Landlock, no privileges; bubblewrap or `flatpak-spawn --no-network` as extra layer where user namespaces work |
| Memory and time limits, kill on exit (S1) | Job Object | `setrlimit` plus Core watchdog; to verify | cgroup v2 where available, otherwise `setrlimit` plus watchdog; `--die-with-parent` with bwrap |
| Pipes and handles | anonymous pipes, handles listed with `PROC_THREAD_ATTRIBUTE_HANDLE_LIST` | inherited descriptors via `posix_spawn`; XPC with `NSFileHandle` is the alternative; use of received descriptors inside the sandbox to verify | inherited descriptors (not close-on-exec); `SCM_RIGHTS` over a Unix socket |
| Worker may read its own binaries | explicit ACL (read and execute for ALL APPLICATION PACKAGES) on the install folder, set by the installer | app bundle, helper signed inside it; to verify | code and libraries are loaded before Landlock is applied; to verify |
| System image decoder / HEIC | WIC with the user's own extensions (which ones are needed: to verify) | ImageIO; inside the sandbox: to verify | libheif only if present on the system, never shipped by us (patents); to verify |
| Global gesture hook (1.2, S11) | `WH_MOUSE_LL` plus `GetAsyncKeyState` for the modifier | `CGEventTap` with Input Monitoring ([Apple forums 707680](https://developer.apple.com/forums/thread/707680)); `NSEvent` global monitors document an Accessibility requirement for key events only; mouse-only: to verify | X11 (XInput2) only; **not available on Wayland** |
| Origin mark (S12) | `Zone.Identifier` with `ZoneId` only | `com.apple.quarantine` | none standard: **not available** |
| File-manager menu (1.1) | Explorer context menu | Finder Quick Action or Service | `.desktop` actions and file-manager extensions (Nautilus, Dolphin service menus); to verify |
| Packaging | Inno Setup per-user installer plus portable ZIP | `.dmg`; signed and notarised if an account exists, else ad-hoc (plan B in ADR 0010) | Flatpak, AppImage, `.deb` (options in the [TRD](TRD.md#future-distribution)) |
| Data, log, settings folders | `%LOCALAPPDATA%\Kalei` | `~/Library/Application Support/Kalei` | `$XDG_DATA_HOME/kalei` (default `~/.local/share/kalei`) |
| Temporary folder (S7) | `%LOCALAPPDATA%\Kalei\tmp` | folder under the per-user temporary directory, Kalei-owned | `$XDG_RUNTIME_DIR/kalei` |

## IPC protocol
Versioned messages between the Core and each Worker on **inherited anonymous pipes** (no named pipe, so no shared name or SID that another process could open); file data travels as inherited handles, never as paths. Messages: `Hello`, `Probe`, `ProbeResult`, `Preview`, `PreviewData`, `Convert`, `Progress`, `Result`, `Error`, `Cancel`. Unknown versions are refused, frame sizes are bounded, the Core's watchdog kills a Worker that sends no frame for 10 s (`Progress` doubles as the heartbeat). Full specification: [ipc-spec.md](ipc-spec.md).
