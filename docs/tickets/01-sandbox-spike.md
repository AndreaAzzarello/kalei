# 01 — Sandbox spike: AppContainer + .NET + libvips from inherited handles, network gate

- **Status**: needs-owner-hardware
- **Size**: L
- **Blocked by**: none
- **Rules**: S1, S3, S5, S7
- **Proofs**: W1, W2, W3, W4, W16 ([ticket-0](../ticket-0.md))
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
Prove, with measurements on the Hyper-V Windows 10 reference machine and on the Windows 11 PC, that a throwaway Worker can convert an image under the Windows sandbox using only inherited handles and with no network. Spike code is throwaway (branch only); only results and decisions return to the documents.

## Acceptance criteria
1. W1: a throwaway Worker in an AppContainer without the network capability converts a PNG to AVIF (or to WebP if AVIF is not available) from inherited handles on Windows 10 22H2 (VM) and on Windows 11 (PC); results recorded.
2. W2: from inside that Worker a TCP connect to a reachable host and a DNS lookup both fail; results recorded on both machines.
3. W3: a seek-heavy TIFF and an AVIF are read through inherited handles; either seekable handles are confirmed or the affected Formats are listed for removal from 1.0.
4. W4: the Worker loads itself, the .NET runtime and libvips from a per-user install folder after read and execute is granted to ALL APPLICATION PACKAGES, with no administrator rights.
5. W16: a hung Worker is killed within 5 s of missing heartbeat; cancel leaves no partial file; after a simulated crash a journal-driven clean-up removes the partial files and nothing else.
6. Every proof is recorded as pass or fail with its numbers in the history; ADR 0003 and requirement S5 are updated. If W1 or W2 fails, the "no network" promise is removed for Windows as S5 says, and the owner confirms the change before it is written.
