# 34 — Spike (1.2): global gesture, Sentinel, false triggers

- **Status**: needs-owner-hardware
- **Size**: L
- **Blocked by**: [12](12-in-app-donut-host.md)
- **Rules**: S11
- **Proofs**: W11g, W12, W13, W14, W15 ([ticket-0](../ticket-0.md))
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
Find out whether the global Donut is feasible and acceptable: speed, memory, false triggers and limits of a mouse-only hook with a small resident process.

## Acceptance criteria
1. W11g: wheel events reach the minimal mouse hook during a file drag, or the one-Face fallback is recorded.
2. W12: gesture to visible overlay is 300 ms or less from a cold start on the reference machine, or the pre-started overlay option is evaluated.
3. W13: the Sentinel's private working set at rest after 10 minutes is 20 MB or less.
4. W14: with the modifier held, 50 text selections, 50 window drags and 50 file-manager drags give no opening on the 100 non-file drags.
5. W15: behaviour with elevated windows is documented; the hook is not removed by the system in a 10-minute stress run.
6. Results go in the history and ADR 0009 is updated; antivirus behaviour is recorded.
