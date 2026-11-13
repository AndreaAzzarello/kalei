# Kalei — IPC specification (v1)

Contract between the Core and a Worker. It is internal to Kalei (not a plugin API, [ADR 0002](adr/0002-module-is-a-process.md)). Requirements it serves: S1, S3, S5, S8, S10 in [architecture.md](architecture.md#security-requirements-normative-the-only-place-they-are-written). Implemented by ticket [07a](tickets/07a-ipc-protocol-and-worker-host.md) (Core side, Worker host, fake launcher) and [07b](tickets/07b-windows-sandbox-launcher.md) (real launcher). **Status: v1, awaiting the owner's review.** Protocol version number: `1`.

## Principle: the Worker is not trusted
A Worker parses untrusted files, so it may be compromised. The Core therefore treats every byte from a Worker as hostile input: it checks length, size per type, rate, syntax, fields, values, sequence and time, **in this order**, and at the first violation it kills the Worker without answering, ends the request with the error code of the table below and logs a closed *reason* (never the frame content, never a path: S10). Nothing a Worker sends is used before it has passed every check. The Worker also validates what it receives from the Core and exits with code 2, without answering, on any violation; its exit code never decides the outcome of a request.

## Transport
- The Core creates **two anonymous pipes** (Core→Worker, Worker→Core) and starts the Worker with only those pipe ends and the file handles listed below inherited. No named pipe exists, so no other process can connect by name or by shared sandbox identity.
- Windows: handles are listed explicitly with `PROC_THREAD_ATTRIBUTE_HANDLE_LIST`. macOS and Linux: inherited descriptors, other descriptors closed; see the [capability table](architecture.md#capability-table-per-os).
- The two pipe handles are the only arguments on the Worker's command line (07b). File data never travels as paths: the Broker opens the Source (read-only) and the Output (exclusive create) before the Worker starts. **Handles must be seekable**; a format that needs more than that is not offered.
- **Handle token.** A file handle is named in a message by its operating-system value (the inherited handle or descriptor number) as a positive integer, opaque to the protocol. The Worker uses it as is. The set of tokens the Worker may use is exactly what the Broker inherited into it; the Core never sends a token it did not inherit.
- Each Worker executable has its own sandbox profile (on Windows its own AppContainer profile; in 1.0 there is one, for the built-in Worker). The install folder is made readable and executable for the sandbox by the installer (capability table).
- **One request per Worker** (S1): a Worker serves exactly one `Probe`, `Preview`, `Convert` or `Canary` and then ends. The cost of starting one is about 46 ms warm (ticket 01, median of 10).

## Framing and encoding
- A frame is a 4-byte little-endian length `N` read as an **unsigned** 32-bit integer, followed by `N` bytes of UTF-8 JSON (the *body*). A length with the top bit set (a "negative" `int32`) is therefore at least 2³¹ and is rejected like any other oversize length.
- **Protocol maximum: 1,048,576 bytes (1 MB)** for `N`. The Core checks `N` before it allocates or reads the body.
- The body is one JSON object, compact, without byte-order mark, and **begins with the exact bytes `{"type":"`** followed by the type name and `"`. This lets the Core read the first 32 bytes, learn the type and apply the cap of that type *before* reading the rest. Both sides write this form; anything else is `BadJson`.
- Other body rules: UTF-8 valid; maximum nesting depth 4; no duplicate keys; no `null` (an optional field that is not set is omitted); names are camelCase and case-sensitive; no field outside the tables; nothing after the closing brace; integers are whole numbers within ±2⁵³−1; `fraction` is a finite number.
- **Size cap per type.** Each type has its own cap on `N` (table below), computed as the next power of two above twice the largest valid message of that type. A conformance test builds the largest valid message of every type and asserts that it is at most half the cap, and that a frame one byte over the cap ends the Worker. Only the raw payload of `PreviewData` reaches the 1 MB maximum.

| Type | Direction | Cap on `N` (bytes) | Why |
|---|---|---|---|
| `Hello` | Worker→Core | 4,096 | worker id plus up to 16 Modules, 1,509 bytes at most |
| `Hello` | Core→Worker | 64 | one integer |
| `Probe` | Core→Worker | 256 | token and Format id |
| `ProbeResult` | Worker→Core | 512 | fixed keys, 215 bytes at most |
| `Preview` | Core→Worker | 128 | token and size |
| `PreviewData` (header) | Worker→Core | 256 | four fields |
| `PreviewData` (raw payload, after the header) | Worker→Core | 1,048,576 | 512 × 512 × 4, the 1 MB maximum |
| `Convert` | Core→Worker | 32,768 | up to 32 options of at most 256 bytes each plus limits, 9,639 bytes at most |
| `Progress` | Worker→Core | 128 | one number |
| `Result` | Worker→Core | 256 | one integer and at most two notice codes |
| `Error` | Worker→Core | 128 | one code |
| `Cancel` | Core→Worker | 64 | no field |
| `Canary` | Core→Worker | 128 | one port |
| `CanaryResult` | Worker→Core | 256 | two outcomes |

The largest valid sizes were computed on 2026-11-13 from the field limits of this document; the caps are initial and are confirmed (or changed) by the conformance test of ticket 07a before the code is merged.
- **Rate cap.** The Core counts frames from the Worker (a `PreviewData` header and its payload count as one frame) at the moment its reader receives them: **more than 100 frames within any window of 1 s ends the Worker** (`FrameRate`). The honest traffic is at most 10 `Progress` per second plus the first and last frame, so the cap is ten times what is needed; the Core keeps only the most recent `Progress` for the UI, so a flood costs the Worker's own time only. The reader is a dedicated task that never waits for the UI, so a slow UI cannot make honest frames arrive in a burst.
- **`PreviewData`.** A normal frame `{"type":"PreviewData","width":W,"height":H,"length":L}` followed on the same pipe by exactly `L` raw bytes. Pixels are BGRA, 8 bits per channel, **not premultiplied alpha**, rows from top to bottom, no padding (stride = `W × 4`), no container or compression. The Core accepts it only if `1 ≤ W,H ≤ maxSize` of the request and `L = W × H × 4` (so `L ≤ 1,048,576`); otherwise `BadValue`. The app shows the pixels without any image decoder (S3).

## Session and sequence
The Core runs one state machine per Worker. The times are in [Watchdog](#watchdog).

| State | Expected from the Worker | Next |
|---|---|---|
| `AwaitHello` | `Hello` | the Core checks the version, answers `Hello`, sends the one request → `Open` |
| `Open` | zero or more `Progress`, then **one** final frame: the answer of the request (`ProbeResult`, `PreviewData`, `Result` or `CanaryResult`) or `Error` | → `Done` |
| `Done` | nothing (the Worker exits by itself; the Core kills it after 3 s anyway) | end |

Every other frame, in any state, is `OutOfSequence`: a valid message in the wrong state, the wrong answer for the request (for example `Result` after `Probe`), a second final frame, a frame after the final one, a message of the wrong direction. After `Cancel` the Core still enforces framing, caps and rate, but ignores the content: the outcome stays `E_CANCELLED` even if a `Result` arrives.

## Messages
Fields not marked *optional* are required. "Core checks" is what the Core does with a Worker message before using it.

### `Hello` (Worker→Core, first frame; then Core→Worker)
| Field | Type | Rule |
|---|---|---|
| `protocol` | int | the one version the sender speaks; must be `1` |
| `worker` | string | build id, `[A-Za-z0-9._+-]{1,64}` (Worker→Core only) |
| `modules` | array, 1–16 | each `{ "id": "[a-z0-9-]{1,32}", "version": semver ≤ 32 }`; the Core checks that each pair matches a loaded manifest of this Worker executable |

```json
{"type":"Hello","protocol":1,"worker":"1.0.0+3f2a9c1","modules":[{"id":"images","version":"1.0.0"},{"id":"data","version":"1.0.0"}]}
```
Core reply (only `protocol`, the version it will use):
```json
{"type":"Hello","protocol":1}
```
**Version negotiation.** The Worker states the single version it speaks; the Core 1.0 accepts only `1`. Any other value (higher, lower, zero) ends the Worker with `E_PROTOCOL`/`BadVersion` and no reply. The `protocol` field of the manifest ([module-manifest.md](module-manifest.md)) must equal it. Any later change to a message or a field raises the number, because unknown fields are refused; a future Core that speaks several versions answers with the Worker's own, if it supports it.

### `Probe` (Core→Worker) and `ProbeResult` (Worker→Core)
| `Probe` field | Type | Rule |
|---|---|---|
| `source` | int | handle token of the Source |
| `candidate` | string, *optional* | Format id from header detection, `[a-z0-9]{1,16}` |

| `ProbeResult` field | Type | Rule |
|---|---|---|
| `format` | string | Format id, `[a-z0-9]{1,16}`; must be a Format of the loaded manifests |
| `confidence` | string | `certain`: the structure was read and is valid; `likely`: signature and header agree but the structure was not read in full |
| `properties` | object | only these keys, all *optional*: `width`, `height` (1–1,000,000), `frames` (1–100,000), `hasAlpha` (bool), `sheets` (1–10,000), `encoding` (`[a-z0-9-]{1,32}`) |

```json
{"type":"Probe","source":412,"candidate":"png"}
{"type":"ProbeResult","format":"png","confidence":"certain","properties":{"width":4096,"height":3072,"frames":1,"hasAlpha":true}}
```
Core checks: pixels (`width × height`) against the Limits table (`E_PIXELS`) before anything else uses the numbers.

### `Preview` (Core→Worker) and `PreviewData` (Worker→Core)
| `Preview` field | Type | Rule |
|---|---|---|
| `source` | int | handle token |
| `maxSize` | int | 16–512; largest width and height of the picture |

```json
{"type":"Preview","source":412,"maxSize":256}
{"type":"PreviewData","width":256,"height":192,"length":196608}
```
Header fields: `width`, `height` (int), `length` (int, bytes that follow); rules under [Framing](#framing-and-encoding).

### `Convert` (Core→Worker)
| Field | Type | Rule |
|---|---|---|
| `from`, `to` | string | Format ids, `[a-z0-9]{1,16}`; the pair is in the manifest |
| `source`, `output` | int | handle tokens (Source read-only, Output exclusive-create, both seekable) |
| `options` | object, 0–32 keys | keys only among the Options of this pair in the manifest; value `bool`, `int` or `string` as the manifest type says (`enum` is a string); `int` within `min`/`max`; `string` at most 256 bytes and matching `pattern` (the manifest validator rejects a `maxLength` above 256); options not set are omitted |
| `limits` | object | keys `pixels`, `depth`, `yamlAliases`, `expansionRatio`, `uncompressedBytes` (all int), copied by the Core from its [Limits table](architecture.md#limits) with the user's changes; **never** from a manifest. Memory and time are not sent: the OS sandbox and the Core enforce them |

```json
{"type":"Convert","from":"png","to":"webp","source":412,"output":420,"options":{"quality":82,"stripMetadata":false},"limits":{"pixels":250000000,"depth":64,"yamlAliases":100,"expansionRatio":100,"uncompressedBytes":1073741824}}
```

### `Progress` (Worker→Core)
| Field | Type | Rule |
|---|---|---|
| `fraction` | number, *optional* | 0 to 1, the Worker's estimate of the share of the work done; not decreasing |
| `bytes` | int, *optional* | bytes of the Source consumed so far; not decreasing; not above the Source size |

At most one of the two. With neither it is a **pure heartbeat**. Both present, out of range, or a value lower than the previous one of the same field: `BadValue`.
```json
{"type":"Progress","fraction":0.42}
{"type":"Progress","bytes":734003200}
{"type":"Progress"}
```

### `Result` (Worker→Core, final)
| Field | Type | Rule |
|---|---|---|
| `outputBytes` | int | bytes written, ≥ 0; from ticket 08 the Core compares it with the real file length (a difference is `BadValue`) |
| `warnings` | array of string, *optional* | notices a Worker may send: `FIRST_FRAME_ONLY`, `LOSSY_ENCODING`; unique |

There is no `ok` field: a `Result` is a success, an `Error` is a failure.
```json
{"type":"Result","outputBytes":1843201,"warnings":["FIRST_FRAME_ONLY"]}
```

### `Error` (Worker→Core, final)
| Field | Type | Rule |
|---|---|---|
| `code` | string | one of the codes a Worker may send (see [Error codes](#error-codes)) |

There is no free-text field: a Worker can read a file name from its handle, and an untrusted text cannot be guaranteed free of paths, so S10 holds by construction. The user message comes from the code alone.
```json
{"type":"Error","code":"E_CORRUPT"}
```

### `Cancel` (Core→Worker)
No field, no answer. The Worker stops and exits; the Core kills it after 3 s if it has not.
```json
{"type":"Cancel"}
```

### `Canary` (Core→Worker) and `CanaryResult` (Worker→Core, final)
Specified here, implemented by ticket [07c](tickets/07c-network-canary.md). No file handles are passed. Both destinations (127.0.0.1:`localPort` and 192.0.2.1:443) are fixed in the Worker and never sent ([network canary](architecture.md#network-canary-s5)).
| Field | Type | Rule |
|---|---|---|
| `localPort` (`Canary`) | int | 1024–65535 |
| `local`, `net` (`CanaryResult`) | object | `{ "outcome": "connected" \| "timeout" \| "error", "code": int }`; `code` (Winsock code, for example 10013, 10051, 10065, 10061) only with `error` |

The Core decides the verdict; it never trusts `connected` without its own listener having seen the connection.
```json
{"type":"Canary","localPort":50123}
{"type":"CanaryResult","local":{"outcome":"timeout"},"net":{"outcome":"error","code":10013}}
```

## Watchdog
| Timer | Value | Starts | On expiry |
|---|---|---|---|
| Handshake | 5 s | the launcher returns the started process | kill, `E_TIME`, reason `HelloTimeout` |
| Heartbeat | 10 s | every received frame (reset each time), while `Open` | kill, `E_TIME`, reason `Heartbeat` |
| Request timeout | `Convert`: the Limits table (2 min + 1 min per 100 MB, at most 15 min); `Probe` 10 s; `Preview` 30 s; `Canary`: set by ticket 07c | request sent | kill, `E_TIME`, reason `Timeout` |
| After `Cancel` | 3 s | `Cancel` sent | kill, outcome `E_CANCELLED` |
| After the final frame | 3 s | final frame received | kill (normal) |

- While a request is open the Worker sends a frame at least every 2 s (`Progress`, with or without a value) and at most 10 per second. The Worker host emits the heartbeat from its own timer. It proves that the process and the pipe are alive, not that the engine progresses: an engine stuck in a loop is ended by the request timeout and the memory cap. A killed Worker's partial Output is removed (journal, lifecycle step 5).
- The Probe and Preview timeouts are initial values; tickets 09a and 14a measure them.
- **Does the 5 s handshake hold on a cold start?** Measured on the reference VM (Hyper-V Windows 10 22H2, 2 vCPU, 4 GB, SATA SSD, Defender on) and on the Windows 11 PC:
  - the spike Worker takes 46–48 ms from process creation to its first message, **warm** (tickets 01 and #47, median of 10); no cold start of the Worker was measured (stated as not verified in [ticket-0](ticket-0.md));
  - the nearest cold figures are those of the UI on the same VM ([ticket 03c](ticket-0.md#results)): the managed code starts at **210 ms** (median), `Main` at **278 ms**, and the whole window shows at **968 ms** (max 993); on the first start after a copy (#47) the window showed at median **1,122.7 ms**, slowest start **1,614.8 ms** (Defender scanning new files). Those figures include the whole of Avalonia; the Worker is smaller but is not ReadyToRun (JIT), and its `Hello` path loads no engine;
  - margin: 5 s is **18 times** the `Main` start (278 ms), **4.5 times** the median of the first start after a copy and **3.1 times** its slowest full-window start. It is an inference from the UI, not a measurement of the Worker;
  - rules that keep the margin: the Worker host sends `Hello` **before** it loads any engine or library (libvips, the largest, is loaded on the first request); `modules` is a constant of the build; the timer starts when the launcher returns, so the one-off creation of the AppContainer profile (07b) is outside it;
  - ticket 07b must measure the time to `Hello` cold on the VM (same method as W7: reboot, 150 s settle, one start, 10 starts, also the first start after install). If the slowest start exceeds 2.5 s (half the timeout), the value is revised with the numbers.

## Malformed, unknown, oversized, out-of-sequence
In every row the Core kills the Worker, sends nothing, and the request ends with the code shown. The checks run in the order of the table.

| # | Case | Reason (log) | Code |
|---|---|---|---|
| 1 | length `N` above 1,048,576 (including a "negative" length) | `FrameTooLarge` | `E_PROTOCOL` |
| 2 | `N` = 0 or below 17 (the shortest valid body is `{"type":"Cancel"}`) | `BadLength` | `E_PROTOCOL` |
| 3 | `N` above the cap of its type (type read from the first 32 bytes) | `FrameTooLargeForType` | `E_PROTOCOL` |
| 4 | body does not start with `{"type":"`; invalid UTF-8 or JSON; depth above 4; duplicate key; `null`; bytes after the object (a prefix shorter than the real JSON shows up here, as truncated JSON) | `BadJson` | `E_PROTOCOL` |
| 5 | type name not in the table, or not allowed in this direction | `UnknownType` | `E_PROTOCOL` |
| 6 | a field that is not in the table | `UnknownField` | `E_PROTOCOL` |
| 7 | a required field is missing | `MissingField` | `E_PROTOCOL` |
| 8 | wrong JSON type, out of range, pattern failure, unknown Format or Module, `Progress` going down or with both fields, `PreviewData` with `length` ≠ `W × H × 4` or size above `maxSize`, `Error` with a code a Worker may not send | `BadValue` | `E_PROTOCOL` |
| 9 | a valid frame in the wrong state, the wrong answer for the request, a second final frame, a frame after the final one | `OutOfSequence` | `E_PROTOCOL` |
| 10 | `Hello.protocol` other than 1 | `BadVersion` | `E_PROTOCOL` |
| 11 | more than 100 frames in 1 s | `FrameRate` | `E_PROTOCOL` |
| 12 | the pipe closes or the process exits before the final frame, including in the middle of a frame or of a `PreviewData` payload | `PipeClosed` | `E_CRASH` |
| 13 | no `Hello` in 5 s; no frame in 10 s; request timeout | `HelloTimeout`, `Heartbeat`, `Timeout` | `E_TIME` |
| 14 | the Worker has not exited 3 s after `Cancel` | (none) | `E_CANCELLED` |
| 15 | a non-zero exit code after a valid final frame | ignored | the final frame decides |

Case 12 is separated from the protocol errors because for the user a converter that crashed on a file is a different event from a converter that spoke wrongly. The log gets one `IPC_END` event per request with the code, the reason, the phase and the duration; no frame content, no path, no file name (S10).

## Error codes
Catalogue for the whole app. A Worker may send **only**: `E_FORMAT`, `E_CORRUPT`, `E_PIXELS`, `E_MEM`, `E_BOMB`, `E_COMPLEX`, `E_UNSUPPORTED_OPTION`, `E_NO_CODEC`, `E_IO`. Every other code is raised by the Core, and a Worker that sends one is ended with `BadValue`.

`E_FORMAT` (not recognised, not supported, or not valid for this conversion, for example XML with a DTD or JSON that is not an array of objects), `E_CORRUPT`, `E_SIZE`, `E_PIXELS`, `E_COUNT`, `E_DEPTH`, `E_MEM`, `E_TIME`, `E_BOMB`, `E_COMPLEX`, `E_UNSUPPORTED_OPTION`, `E_NO_CODEC` (e.g. HEIC without the system extension), `E_SANDBOX` (Worker could not be started under the sandbox), `E_PROTOCOL`, `E_CRASH` (the Worker ended or closed its pipe before answering; new in v1), `E_CANCELLED`, `E_IO` (Output could not be written).

**Notices** are not errors: they never stop a Conversion and are shown as a note. A Worker may send only `FIRST_FRAME_ONLY` and `LOSSY_ENCODING` (characters the output encoding cannot hold were replaced); the Core raises `MISMATCH` (content differs from extension), `NO_ORIGIN_MARK` (the destination cannot hold the origin mark) and `NETWORK_ISOLATION_UNPROVEN` (the network canary could not prove the block on this PC right now; neutral wording, at most once per session, not tied to one Conversion; Conversions work normally).

Each error and each notice maps to one plain-language message in the UI (catalogue kept with the string resources, English and Italian); a test checks that no code or notice lacks a message, `E_CRASH` included.

## Decisions taken in v1
| # | Decision | Reason |
|---|---|---|
| D1 | One request per Worker, also for `Probe` and `Preview` | S1; 46 ms to start warm; revisit with 09a/14a if a large Batch makes it slow |
| D2 | Handle token = the OS handle value, opaque, in the message | no list to keep in sync; same on Windows and POSIX |
| D3 | No free text in `Error` | a path cannot be ruled out of untrusted text; S10 by construction |
| D4 | New code `E_CRASH` | a crash is not a protocol error for the user |
| D5 | Heartbeat from a timer in the Worker host | the engine's own progress can be silent for long; the hang is caught by the request timeout and the memory cap |
| D6 | A non-monotonic or double `Progress` is `E_PROTOCOL` | strict and testable |
| D7 | Probe 10 s, Preview 30 s | initial values, measured in 09a and 14a |
| D8 | `PreviewData` pixels are not premultiplied | Avalonia shows them directly (`AlphaFormat.Unpremul`) |
| D9 | Size cap per type and a rate cap of 100 frames per second, kill and `E_PROTOCOL` (`FrameTooLargeForType`, `FrameRate`) | a compromised Worker gets neither memory nor CPU from the Core; honest traffic is far below both |

Values to confirm by test, not decisions: the caps of the size table, the 100 frames per second, the Hello margin on a cold Worker (07b).
