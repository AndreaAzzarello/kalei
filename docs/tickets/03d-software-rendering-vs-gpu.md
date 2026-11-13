# 03d — Software rendering or GPU: start-up and Donut on the Windows 11 PC

- **Status**: needs-owner-hardware
- **Size**: S
- **Blocked by**: –
- **Rules**: none
- **Stories**: none (constraint, spike or infrastructure)
- **Proofs**: W7, W10 ([ticket-0](../ticket-0.md))
- **Manual check**: AC 4 (smoothness) is also looked at by eye on the Windows 11 PC.
- **Spec**: [ADR 0001](../adr/0001-avalonia-net10-aot-ui-only.md), [ADR 0008](../adr/0008-radial-donut-component.md)

## What to build
Ticket [03c](03c-w7-cold-start-breakdown.md) found that software rendering (`Win32PlatformOptions.RenderingMode = [Software]`, no ANGLE/EGL) lowers the cold start on the reference VM from a median of 968 ms to 811 ms (paired difference about -150 ms over 5 rounds): the platform set-up falls from 280 ms to 153 ms. The VM has no GPU, so nothing is known about the Donut on a PC with one. **Owner decision of 2026-11-12: do not adopt it yet.** This ticket compares GPU (Avalonia's default) and software rendering on the owner's Windows 11 PC with a GPU, changing nothing else.

## Acceptance criteria
1. A switch for the rendering mode is added to the throwaway spike windows (`spike/03a-build` for start-up, `spike/04-donut` for the Donut; code stays on its branch, never merged). The two modes use the same build and the same machine; the GPU, driver, display refresh rate and scaling are recorded in the report.
2. (a) Warm start-up of the window: median of 10 runs of the second-frame time of ticket 03c, for each mode.
3. (b) W10 latency (first drag-enter to the second frame of the overlay, as in ticket [04](04-in-app-donut-spike.md)): median of 10 cold rows at 100% and at 300% scaling, for each mode.
4. (c) Smoothness of the ring rotation and of the Category colour change, for each mode: frame times measured over a fixed 5 s (median, 95th percentile, maximum, count of frames longer than 33 ms), the method written in the report, and the result looked at by eye. Meaning of "smooth" (confirmed by the owner, 2026-11-13): at most 1% of frames longer than 33 ms and the median frame time within 10% of the screen's refresh period.
5. (d) CPU use of the process during the same animation: CPU time over the 5 s as a percentage of one core, for each mode.
6. **Rule**: software rendering is adopted only if the Donut latency stays at 100 ms or less at 100% and at 300% **and** the animation is smooth by criterion 4. Otherwise GPU stays the default and the ~150 ms are not taken from rendering.
7. The alternative is evaluated and written up: an automatic choice (software only when there is no usable GPU), with Avalonia's documentation as the source. Facts already read ([Avalonia docs, Windows](https://github.com/avaloniaui/avalonia-docs/blob/main/docs/platform-specific-guides/windows.md)): `RenderingMode` is an ordered list and the first mode that initialises is used; the default is `AngleEgl` then `Software`; Avalonia falls back to software by itself when GPU acceleration is unavailable (remote sessions, virtual machines without GPU passthrough). To find out: whether ANGLE on the VM initialised on a software Direct3D adapter (so that the VM did not use `Software` at all and the 150 ms are the cost of ANGLE itself), and how to tell a software adapter from a real one before choosing the list.
8. The result and the decision are written in HISTORY and in the W7/W10 results of ticket-0; ADR 0001 or 0008 are updated only if the mode changes.
