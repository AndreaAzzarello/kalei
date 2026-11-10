# 07 — Windows sandbox launcher and Worker IPC host

- **Status**: ready-for-agent
- **Size**: L
- **Blocked by**: [01](01-sandbox-spike.md), [05](05-solution-skeleton.md)
- **Rules**: S1, S2, S5
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
The Core can start a Worker under the Windows sandbox, talk to it over the specified protocol, and always end it: on success, error, cancel, timeout, hang or app exit.

## Acceptance criteria
1. The Windows sandbox launcher starts a Worker in a Job Object and an AppContainer without the network capability, with inherited anonymous pipes and the listed handles only.
2. The messages Hello, Convert, Progress, Result, Error and Cancel are framed as in the IPC spec; an unknown protocol version, an unknown message or a frame over 1 MB ends the Worker with E_PROTOCOL.
3. The watchdog kills a Worker that sends nothing for 5 s; the timeout and memory limits from the Limits table end it with E_TIME and E_MEM; Workers are killed when the app exits (S1).
4. A fake launcher and a fake Worker are available for tests of everything above the launcher.
5. A test attempts a TCP connect and a DNS lookup from inside a real Worker and both must fail (S5); it runs on the Windows reference machine.
6. If the sandbox cannot start, the result is E_SANDBOX and nothing is converted; Kalei never runs a Worker unsandboxed.
