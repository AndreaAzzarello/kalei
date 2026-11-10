# 01 — Sandbox spike: AppContainer + .NET + libvips from inherited handles, network gate

- **Status**: needs-owner-hardware
- **Size**: M
- **Blocked by**: –
- **Rules**: S1, S3, S5, S7
- **Stories**: none (constraint, spike or infrastructure)
- **Proofs**: W1, W2, W3, W4 ([ticket-0](../ticket-0.md))
- **Spec**: [ticket-0, Part A](../ticket-0.md#part-a--windows-blocks-10)

## What to build
Prove, with measurements on the main reference machine (Hyper-V, Windows 10 22H2) and on the Windows 11 PC, that a throwaway Worker can convert an image under the Windows sandbox using only inherited handles and with no network. Spike code is throwaway (branch only); only results and decisions return to the documents. The watchdog and cancel proof (W16) is run in tickets [07b](07b-windows-sandbox-launcher.md) and [08](08-broker-and-output-writing.md).

## Acceptance criteria
1. W1: a throwaway Worker in an AppContainer without the network capability converts a PNG to AVIF (or to WebP if AVIF is not available) from inherited handles on Windows 10 22H2 (VM) and on Windows 11 (PC); results recorded.
2. W2: from inside that Worker a TCP connect to a reachable host and a DNS lookup both fail; results recorded on both machines.
3. W3: a seek-heavy TIFF and an AVIF are read through inherited handles; either seekable handles are confirmed or the affected Formats are listed for removal from 1.0.
4. W4: the Worker loads itself, the .NET runtime and libvips from a per-user install folder after read and execute is granted to ALL APPLICATION PACKAGES (the group that AppContainer processes belong to), with no administrator rights.
5. Every proof is recorded as pass or fail with its numbers in HISTORY; ADR 0003 and requirement S5 are updated. If W1 or W2 fails, the "no network" promise is removed for Windows as S5 says, and the owner confirms the change before it is written.
