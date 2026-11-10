# 34 — Spike (1.2): global gesture, Sentinel, wheel, false triggers

- **Status**: needs-owner-hardware
- **Size**: L
- **Blocked by**: [12b](12b-in-app-donut-host.md)
- **Rules**: S11
- **Stories**: none (constraint, spike or infrastructure)
- **Proofs**: W11, W11g, W12, W13, W14, W15, L5, M4 ([ticket-0](../ticket-0.md))
- **Spec**: [ticket-0, Part A2 and ADR 0009](../ticket-0.md#part-a2--windows-global-gesture-spike-for-12-does-not-block-10)

## What to build
Find out whether the global Donut and the Donut's back Face are feasible and acceptable: speed, memory, false triggers and limits of a mouse-only hook with a small resident process, on each operating system where it could ship.

## Acceptance criteria
1. W11: mouse-wheel events reach the in-app Donut during a drag, or the Tab-only fallback is recorded; W11g: wheel events reach the minimal mouse hook during a file drag, or the one-Face fallback is recorded.
2. W12: gesture to visible overlay is 300 ms or less from a cold start on the main reference machine, or the pre-started overlay option is evaluated.
3. W13: the Sentinel's private working set at rest after 10 minutes is 20 MB or less.
4. W14: with the modifier held, 50 text selections, 50 window drags and 50 file-manager drags give an opening only on file drags (0 openings on the other 100).
5. W15: behaviour with elevated windows is documented; the hook is not removed by the system in a 10-minute stress run.
6. L5: the global hook works on X11 and is clearly unavailable on Wayland (Ubuntu and Fedora VMs); M4: an event tap with Input Monitoring works on the Mac mini and the need to grant it again after an ad-hoc rebuild is recorded.
7. Antivirus behaviour of the Sentinel is recorded (Microsoft Defender and one other free antivirus). Results go in HISTORY and ADR 0009 is updated.
