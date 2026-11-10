# 04 — In-app Donut spike: drag over an Avalonia window, latency, wheel, two monitors

- **Status**: needs-owner-hardware
- **Size**: M
- **Blocked by**: none
- **Rules**: S3
- **Proofs**: W10, W11 ([ticket-0](../ticket-0.md))
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
Prove that an ordinary drag of files over an Avalonia window can open a radial overlay fast enough, learn what is known about the files before the drop, and find out whether the mouse wheel works during the drag.

## Acceptance criteria
1. W10: dragging files over an Avalonia window opens an overlay at the cursor; the time from the first drag event to the visible overlay is 100 ms or less on the Hyper-V reference machine.
2. It is recorded whether the file list and the Format are known before the drop; if only at the drop, the "analysing" behaviour is defined.
3. W11: mouse-wheel events reach the overlay during the drag, or the fallback (Tab only) is recorded.
4. On the Windows 11 PC with two monitors and different DPI the overlay is correctly placed on both screens and is not clipped.
5. A drop is reported to the file manager as Copy and the source file is unchanged afterwards (S4).
6. Results are written in the history; ADR 0008 is updated if a fallback is taken.
