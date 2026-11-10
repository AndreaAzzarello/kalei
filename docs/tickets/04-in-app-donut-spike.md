# 04 — In-app Donut spike: drag over an Avalonia window, latency, two monitors

- **Status**: needs-owner-hardware
- **Size**: M
- **Blocked by**: –
- **Rules**: S3, S4
- **Stories**: none (constraint, spike or infrastructure)
- **Proofs**: W10 ([ticket-0](../ticket-0.md))
- **Spec**: [ticket-0, Part A](../ticket-0.md#part-a--windows-blocks-10)

## What to build
Prove that an ordinary drag of files over an Avalonia window can open a radial overlay fast enough, and learn what is known about the files before the drop.

## Acceptance criteria
1. W10: dragging files over an Avalonia window opens an overlay at the cursor; the time from the first drag-enter event received by the window to the first frame of the overlay is 100 ms or less (median of 10 cold runs) on the main reference machine.
2. It is recorded whether the file list and the Format are known before the drop; if only at the drop, the "analysing" behaviour is defined.
3. On the Windows 11 PC with two monitors, at 100%, 150% and 200% scaling and across the edge between the monitors, the overlay is correctly placed and not clipped.
4. A drop is reported to the file manager as Copy and the source file is unchanged afterwards (S4).
5. Results are written in HISTORY; ADR 0008 is updated if a fallback is taken. (The wheel proof W11 is in ticket [34](34-global-gesture-spike.md): 1.0 has no back Face.)
