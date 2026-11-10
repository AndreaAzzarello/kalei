# Kalei — IPC specification (draft v0)

Contract between the Core and a Worker. It is internal to Kalei (not a plugin API, [ADR 0002](adr/0002-module-is-a-process.md)). Requirements it serves: S1, S3, S5, S8 in [architecture.md](architecture.md#security-requirements-normative-the-only-place-they-are-written). Field details are fixed in the spec for Ticket 1; this document fixes the shape.

## Transport
- The Core creates **two anonymous pipes** (Core→Worker, Worker→Core) and starts the Worker with only those pipe ends and the file handles listed below inherited. No named pipe exists, so no other process can connect by name or by shared sandbox identity.
- Windows: handles are listed explicitly with `PROC_THREAD_ATTRIBUTE_HANDLE_LIST`. macOS and Linux: inherited descriptors, other descriptors closed; see the [capability table](architecture.md#capability-table-per-os).
- File data never travels as paths: the Broker opens the Source (read-only), and the Output (exclusive create) before the Worker starts. **Handles must be seekable** (random access); a format that needs more than that is not offered.
- Each Worker executable has its own sandbox profile (on Windows its own AppContainer profile; in 1.0 there is one, for the built-in Worker), so one executable's Worker cannot reach another's resources. The install folder is made readable and executable for the sandbox by the installer (capability table).

## Framing and encoding
- A frame is a 4-byte little-endian length followed by a UTF-8 JSON object with a `type` field. Maximum frame size **1 MB**; a larger frame is a protocol error and the Worker is killed.
- Binary payloads are sent as the single exception: type `PreviewData`, a frame header with `width`, `height` and the byte length, then the raw pixels (BGRA, 8 bits per channel, uncompressed, no container) on the same pipe. `width` and `height` must not exceed the maximum preview size declared in the Limits; the Core accepts the payload only if the length equals `width × height × 4` and otherwise ends the Worker with `E_PROTOCOL`. The app shows the pixels without any image decoder (S3).

## Messages
| Type | Direction | Fields (draft) |
|---|---|---|
| `Hello` | Worker→Core, then Core→Worker | `protocol` (integer), `worker` (build id), `modules` (ids and versions); the Core answers with the protocol it will use. Handshake timeout 5 s |
| `Probe` | Core→Worker | `handle` (index of the inherited Source handle), `candidate` (from header detection); answer `ProbeResult`: `format`, `confidence`, `properties` (size, pixels, sheets…) or `Error` |
| `Preview` | Core→Worker | `handle`, `maxSize`; answer `PreviewData` (raw BGRA pixels) or `Error` |
| `ProbeResult` | Worker→Core | answer to `Probe` (fields as listed under `Probe`) |
| `PreviewData` | Worker→Core | answer to `Preview`: frame header with `width`, `height` and the byte length, then the raw BGRA pixels (see Framing) |
| `Convert` | Core→Worker | `from`, `to`, `options` (key/value, validated against the module manifest), `source` and `output` handle indexes, `limits` (copied by the Core from its own table) |
| `Progress` | Worker→Core | `fraction` (0–1) or `bytes`, or neither (then it is a pure heartbeat); sent at least every 2 s while a request is open, doubles as heartbeat |
| `Result` | Worker→Core | `ok`, `outputBytes`, `warnings` (codes, e.g. `FIRST_FRAME_ONLY`) |
| `Error` | Worker→Core | `code` (from the table below), `detail` (never a path or file name) |
| `Cancel` | Core→Worker | none; the Worker stops and exits; the Core kills it after 3 s anyway |
| `Canary` | Core→Worker | `localPort` (the listener the Core opened on 127.0.0.1), `external` (whether the two external probes are enabled); no file handles are passed. The destinations are fixed in the Worker, not sent ([network canary](architecture.md#network-canary-s5)) |
| `CanaryResult` | Worker→Core | per probe (`local`, `tcp`, `dns`): `refused`, `timeout` or `connected`; the Core decides the verdict |

Unknown `protocol` versions are refused at `Hello`. Unknown message types or fields are protocol errors.

## Watchdog
The Core kills a Worker that sends no frame for 10 s while a request is open, or that exceeds the timeout or memory limit of the [Limits table](architecture.md#limits). A killed Worker's partial Output is removed (journal, lifecycle step 5).

## Error codes
`E_FORMAT` (not recognised, not supported, or not valid for this conversion, for example XML with a DTD or JSON that is not an array of objects), `E_CORRUPT`, `E_SIZE`, `E_PIXELS`, `E_COUNT`, `E_DEPTH`, `E_MEM`, `E_TIME`, `E_BOMB`, `E_COMPLEX`, `E_UNSUPPORTED_OPTION`, `E_NO_CODEC` (e.g. HEIC without the system extension), `E_SANDBOX` (Worker could not be started under the sandbox), `E_PROTOCOL`, `E_CANCELLED`, `E_IO` (Output could not be written). **Notices** are not errors: they never stop a Conversion and are shown as a note: `MISMATCH` (content differs from extension), `FIRST_FRAME_ONLY`, `LOSSY_ENCODING` (characters the output encoding cannot hold were replaced), `NO_ORIGIN_MARK` (the destination cannot hold the origin mark), `NETWORK_ISOLATION_UNPROVEN` (the network canary did not prove the block on this PC; shown once per session, not tied to one Conversion). Each error and each notice maps to one plain-language message in the UI (catalogue kept with the string resources, English and Italian); a test checks that no code or notice lacks a message.
