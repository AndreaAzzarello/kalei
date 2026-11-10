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

## IPC protocol
Versioned messages over a named pipe created by the Core with an ACL restricted to the Worker's AppContainer SID: `Hello(version)`, `Convert(format pair, options)`, `Progress`, `Result`, `Error(code)`, `Cancel`. Unknown versions are refused. Message sizes are bounded. Details to be fixed in the spec of Ticket 0.

## Risks to settle in Ticket 0
AppContainer + .NET runtime + NetVips.Native + WIC all working together; handle passing into AppContainer; real installer size; AVIF writing and GIF (cgif) present in the prebuilt libvips.
