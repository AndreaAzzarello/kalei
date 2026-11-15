# 07b — Windows sandbox launcher: Job Object, AppContainer, first-run set-up

- **Status**: ready-for-agent
- **Size**: L
- **Blocked by**: [01](01-sandbox-spike.md), [05](05-solution-skeleton.md), [07a](07a-ipc-protocol-and-worker-host.md)
- **Rules**: S1, S2, S5, S7
- **Stories**: 8, 41, 43 ([user stories](../spec/1.0-windows.md#user-stories))
- **Proofs**: W16, Worker part ([ticket-0](../ticket-0.md)); the partial-file and journal part is in ticket [08](08-broker-and-output-writing.md)
- **Manual check**: AC 3 is run on the main reference machine (Hyper-V) and on the Windows 11 PC by the owner or on their VMs.
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
The Core can start a Worker under the Windows sandbox and always end it: on success, error, cancel, timeout, hang or app exit. This ticket also owns the first-run sandbox set-up that the installer cannot do.

## Acceptance criteria
1. The Windows sandbox launcher starts a Worker in a Job Object and an AppContainer without the network capability, with inherited anonymous pipes and only the listed handles.
2. The memory and time caps of the Limits table end a Worker with E_MEM and E_TIME; Workers are killed when the app exits (S1); a hung Worker is killed within 10 s of missing heartbeat (W16).
3. A TCP connect and a DNS lookup from inside a real Worker both fail (S5); the test runs on the main reference machine and on the Windows 11 PC. The same check runs on every user PC at every start-up in ticket [07c](07c-network-canary.md).
4. If the sandbox cannot start, the result is E_SANDBOX and nothing is converted; Kalei never runs a Worker unsandboxed.
5. First run: the AppContainer profile is created; if the folder access for the sandbox is missing (portable ZIP) and the folder is one the user owns, the app sets it; a clean-up command of the app removes both; if the set-up is impossible a clear message is shown and no conversion starts.
6. W16, Worker part: Cancel ends the Worker (Cancel message, then kill after 3 s) and leaves nothing running; the partial Output file and the journal are checked in ticket [08](08-broker-and-output-writing.md) (criteria 3 and 4), which owns that half of W16.

## Known limits
Found by the security review of this ticket (low severity, not fixed here):
- `SandboxSetup.Remove()` removes every explicit allow rule of ALL APPLICATION PACKAGES on the Worker folder, including one an installer made; ticket [31](31-per-user-installer.md) (criterion 8) owns the fix, with a shared marker.
- The window in which the Worker's pipe ends are inheritable is closed only against other Kalei launches (a process-wide lock). Another `CreateProcess` with handle inheritance in the same process during that window could pass them on; the app starts no other process today, and any ticket that does must take the same lock.
- A Worker folder owned by the Administrators group (not by the user) gives `FolderNotOwned`: no conversion starts and nothing is changed. The installer grants the access, so this concerns the portable ZIP only.
