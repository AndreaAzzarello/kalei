# 35 — Spike: Linux sandbox, Donut and packaging (L1–L6)

- **Status**: needs-owner-hardware
- **Size**: L
- **Blocked by**: none
- **Rules**: S1, S5
- **Proofs**: L1, L2, L3, L4, L5, L6 ([ticket-0](../ticket-0.md))
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
Decide whether Linux can ship in 1.x and with which promise, by testing the sandbox, the Donut and the packaging on Ubuntu 24.04 and the current Fedora.

## Acceptance criteria
1. L1: a Worker with `no_new_privs`, a seccomp filter allowing only local sockets and Landlock with no paths converts a PNG to WebP; a TCP connect, a UDP send and a DNS lookup all fail; opening any path outside the inherited descriptors fails.
2. L2: the .NET runtime and libvips work under those restrictions, and the start-up order is documented.
3. L3: bubblewrap as an extra layer is tried as a system program, inside Flatpak and from an AppImage on Ubuntu 24.04; failure is detected and falls back to L1.
4. L4: the In-app Donut opens under X11 and Wayland; a drag from the file manager gives a usable file list.
5. L5: the global hook works on X11 and is clearly unavailable on Wayland.
6. L6: Flatpak and AppImage builds start on clean VMs; sizes and the FUSE need are recorded.
7. The decision for each distribution is recorded, and where the network block cannot be shown the "no network" promise is dropped for Linux.
