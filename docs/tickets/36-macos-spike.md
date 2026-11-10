# 36 — Spike: macOS sandbox, Gatekeeper and Donut (M1–M6)

- **Status**: needs-owner-hardware
- **Size**: L
- **Blocked by**: none
- **Rules**: S1, S5
- **Proofs**: M1, M2, M3, M4, M5, M6 ([ticket-0](../ticket-0.md))
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
Decide whether macOS can ship in 1.x without depending on an Apple account, by testing an ad-hoc signed sandboxed Worker on the Mac mini M1.

## Acceptance criteria
1. M1: a Worker signed ad hoc with the app sandbox and inherit entitlements, without the network entitlement, started with posix_spawn, converts a PNG to WebP; a TCP connect and a DNS lookup both fail; it reads and writes only the inherited descriptors (otherwise XPC with file handles is tried).
2. M2: ImageIO and HEIC work inside the sandbox, or HEIC is not offered on macOS.
3. M3: the ad-hoc build opens through Privacy & Security → Open Anyway as documented.
4. M4: an event tap with Input Monitoring works, and the need to grant it again after an ad-hoc rebuild is recorded.
5. M5: a drag from the Finder into the In-app Donut works.
6. M6: only if the owner's Apple developer account is active, Developer ID signing and notarisation of the same build work and do not change the sandbox result.
7. Where the network block cannot be shown, the "no network" promise is dropped for macOS.
