# 07a — IPC protocol and Worker host: messages, watchdog decisions, fake launcher

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [05](05-solution-skeleton.md)
- **Rules**: S1, S2, S3
- **Stories**: none (constraint, spike or infrastructure)
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
The Core can talk to a Worker over the specified protocol and decide when to end it, all testable with a fake launcher and a fake Worker, before any operating-system sandbox exists.

## Acceptance criteria
1. The IPC specification is updated from draft to v1 before the code is written: every message has a complete field list and a JSON example; the values of confidence and the keys of properties in ProbeResult, the header of PreviewData, the Core reply to Hello, the keys of options (from the manifest) and the meaning of fraction and bytes are defined; the owner reviews it.
2. The messages Hello, Probe, ProbeResult, Preview, PreviewData, Convert, Progress, Result, Error and Cancel are framed as in the IPC spec; an unknown protocol version, an unknown message type or field, or a frame over 1 MB ends the Worker with E_PROTOCOL.
3. Progress is sent at least every 2 s while a request is open (with neither fraction nor bytes it is a pure heartbeat); the watchdog ends a Worker that sends no frame for 10 s.
4. Cancel asks the Worker to stop; the Core kills it if it has not exited after 3 s.
5. A fake launcher and a fake Worker are available for tests of everything above the real launcher; the tests were written first.
6. Every Error carries a code from the catalogue and never a path or file name.
