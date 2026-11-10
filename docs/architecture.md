# Kalei — Architecture

Decisions behind this are in [docs/adr](adr/). Terms: [GLOSSARY.md](../GLOSSARY.md).

## Shape

```
Kalei.App (Avalonia, Native AOT)        UI only: states, drop zone, accent, settings
   │ calls
Kalei.Core (pure .NET, no UI)           format detection, queue, limits, module manager,
   │                                    output naming, temp/log policy, Broker
   │ versioned IPC (pipes) + handles
Kalei.Worker.* (one process per Conversion)   sandboxed; hosts one Module's Engines
   ├─ Kalei.Worker.Core  (built-in: images, data, text)
   └─ <optional module workers>              installed to %LOCALAPPDATA%\Kalei\modules
```

```
Kalei.Sentinel (1.2 only, opt-in)       mouse hook + overlay launcher, ≤ 20 MB at rest
   │ starts on gesture
Kalei.App overlay host (global Donut)   same Donut component as the in-app host
```

## Donut component (ADR 0008)
`Kalei.Donut` is a UI-independent library: slice layout, angles, hit-testing, face logic (front/back), disabled-slice reasons, all unit-testable. A thin Avalonia control draws it. Two hosts: the **in-app host** (1.0) shows it as an overlay inside the main window during an OLE drag-over; the **global host** (1.2) shows it in a transparent, topmost, non-activating window. The Donut asks the Core which target Formats suit the dragged files (format detection from content once the first `DragEnter` delivers the file list) and which Modules are installed; it returns the chosen Format to the Core's queue, which starts the Conversion as usual. A drop is always reported to the file manager as *Copy*, so a source is never removed.

## Sentinel and hook (ADR 0009, 1.2)
`Kalei.Sentinel` is a minimal separate process: a `WH_MOUSE_LL` hook and `GetAsyncKeyState` on the configured modifier while a drag is in progress. No keyboard hook, no keystroke recording, no network, nothing logged. On the gesture it starts the overlay host and hands it the cursor position over a local pipe restricted to the current user. Residency and start at sign-in are opt-in. Open measurements (Ticket 0): cold-start latency to a visible overlay, RAM at rest, wheel events during an OLE drag, antivirus behaviour.

## Browser drops
Only data the browser hands over as bytes (virtual file or its own temporary copy) is accepted, through the same Broker path as any file. A dropped link without bytes is refused with "Save the file first". No download code exists in Kalei apart from the approved Module install. The source's Mark of the Web is copied onto the Output file.

## Core responsibilities
- **Format detection** by magic bytes and structure, never by extension alone; mismatches are reported to the user.
- **Queue**: ordered Conversions of a Batch, bounded parallelism, cancellation, per-file result.
- **Module manager**: reads each `module.json` (input/output Formats, Options, Limits, version), builds the Conversion matrix at runtime, handles install/uninstall and hash verification ([ADR 0004](adr/0004-optional-modules-embedded-hashes.md)).
- **Broker**: opens the Source file read-only and creates the Output file; passes handles to the Worker; the Worker never sees a path ([ADR 0003](adr/0003-worker-sandbox-and-handle-passing.md)).
- **Output naming**: sanitises names, rejects path traversal and reserved Windows names, resolves clashes to `name (1).ext`, never overwrites.
- **Limits** enforcement before and during Conversion.

## Conversion lifecycle
1. Core detects the Format and checks Limits (size, count, depth).
2. Core picks the Module; if Optional and missing, the user is asked (consent) — otherwise skip.
3. Broker opens handles; Core starts a Worker in a Job Object + AppContainer with a timeout and memory cap.
4. Worker converts over streams and reports progress/result over IPC.
5. Core writes the Output file atomically (temporary name in the Output folder, then rename), or removes the partial file on cancel/error/timeout.
6. Worker process is always terminated, success or not.

## Security requirements (normative; mirrored in CLAUDE.md)
1. Each Conversion runs in its own Worker process with timeout, memory cap and cancel.
2. Limits on size and count; protection against zip bombs (expansion ratio, total size, nested archives, XLSX included) and malformed or malicious files.
3. Real type from content; the extension is only a hint.
4. Output names and paths sanitised; no traversal; no silent overwrite.
5. No network access for Workers (AppContainer without network capability). The only allowed connection is the user-approved install of an Optional module from the fixed source with a verified hash.
6. No automatic update without consent.
7. Temporary files only in a dedicated Kalei folder; always deleted on end, error or cancel; stale ones removed at start-up.
8. XML: DTD and external entities disabled. YAML: no arbitrary type instantiation. HTML output sanitised. CSV→XLSX never writes formulas.
9. Logs: no paths, no file names.
10. Input: never a keyboard hook; the global gesture uses a mouse hook plus the state of one chosen modifier key, and nothing typed is recorded (ADR 0009).
11. Conversion graph: the Core models conversions as a graph (at most 3 steps per route) so multi-step conversions can be offered later; every step runs in its own Worker.

## IPC protocol
Versioned messages over a named pipe created by the Core with an ACL restricted to the Worker's AppContainer SID: `Hello(version)`, `Convert(format pair, options)`, `Progress`, `Result`, `Error(code)`, `Cancel`. Unknown versions are refused. Message sizes are bounded. Details to be fixed in the spec of Ticket 0.

## Risks to settle in Ticket 0
AppContainer + .NET runtime + NetVips.Native + WIC all working together; handle passing into AppContainer; real installer size; AVIF writing and GIF (cgif) present in the prebuilt libvips.
