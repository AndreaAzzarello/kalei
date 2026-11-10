# Kalei — IPC specification (draft v0)

Contract between the Core and a Worker. It is internal to Kalei (not a plugin API, [ADR 0002](adr/0002-module-is-a-process.md)). Requirements it serves: S1, S3, S5, S8 in [architecture.md](architecture.md#security-requirements-normative-the-only-place-they-are-written). Field details are fixed in the spec for Ticket 1; this document fixes the shape.

## Transport
- The Core creates **two anonymous pipes** (Core→Worker, Worker→Core) and starts the Worker with only those pipe ends and the file handles listed below inherited. No named pipe exists, so no other process can connect by name or by shared sandbox identity.
- Windows: handles are listed explicitly with `PROC_THREAD_ATTRIBUTE_HANDLE_LIST`. macOS and Linux: inherited descriptors, other descriptors closed; see the [capability table](architecture.md#capability-table-per-os).
- File data never travels as paths: the Broker opens the Source (read-only), and the Output (exclusive create) before the Worker starts. **Handles must be seekable** (random access); a format that needs more than that is not offered.
- Each Module has its own sandbox profile (on Windows its own AppContainer profile), so one Module's Worker cannot reach another's resources. The install folder is made readable and executable for the sandbox by the installer (capability table).

## Framing and encoding
- A frame is a 4-byte little-endian length followed by a UTF-8 JSON object with a `type` field. Maximum frame size **1 MB**; a larger frame is a protocol error and the Worker is killed.
- Binary payloads (preview or probe images) are sent as the single exception: type `PreviewData`, a frame header with the byte length, then at most 4 MB of PNG bytes on the same pipe.

## Messages
| Type | Direction | Fields (draft) |
|---|---|---|
| `Hello` | Worker→Core, then Core→Worker | `protocol` (integer), `worker` (build id), `modules` (ids and versions); the Core answers with the protocol it will use. Handshake timeout 5 s |
| `Probe` | Core→Worker | `handle` (index of the inherited Source handle), `candidate` (from header detection); answer `ProbeResult`: `format`, `confidence`, `properties` (size, pixels, sheets…) or `Error` |
| `Preview` | Core→Worker | `handle`, `maxSize`; answer `PreviewData` (bounded PNG) or `Error` |
| `Convert` | Core→Worker | `from`, `to`, `options` (key/value, validated against the module manifest), `source` and `output` handle indexes, `limits` (copied by the Core from its own table) |
| `Progress` | Worker→Core | `fraction` (0–1) or `bytes`; sent at least every 5 s, doubles as heartbeat |
| `Result` | Worker→Core | `ok`, `outputBytes`, `warnings` (codes, e.g. `FIRST_FRAME_ONLY`) |
| `Error` | Worker→Core | `code` (from the table below), `detail` (never a path or file name) |
| `Cancel` | Core→Worker | none; the Worker stops and exits; the Core kills it after 3 s anyway |

Unknown `protocol` versions are refused at `Hello`. Unknown message types or fields are protocol errors.

## Watchdog
The Core kills a Worker that sends no frame for 5 s while a request is open, or that exceeds the timeout or memory limit of the [Limits table](architecture.md#limits). A killed Worker's partial Output is removed (journal, lifecycle step 5).

## Error codes
`E_FORMAT` (not recognised or not supported), `E_MISMATCH` (content differs from extension, informational), `E_CORRUPT`, `E_SIZE`, `E_PIXELS`, `E_COUNT`, `E_DEPTH`, `E_MEM`, `E_TIME`, `E_BOMB`, `E_COMPLEX`, `E_UNSUPPORTED_OPTION`, `E_NO_CODEC` (e.g. HEIC without the system extension), `E_SANDBOX` (Worker could not be started under the sandbox), `E_PROTOCOL`, `E_CANCELLED`, `E_IO` (Output could not be written). Each maps to one plain-language message in the UI (catalogue kept with the string resources, English and Italian); a test checks that no code lacks a message.
