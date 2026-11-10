# 11 — Donut library: geometry, hit-testing, faces, format intersection

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [05](05-solution-skeleton.md)
- **Rules**: none
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
A UI-free library that decides everything about the Donut: where the Slices are, which one the cursor is over, what is on each Face, and which Formats are valid for a mix of files.

## Acceptance criteria
1. Slice layout and angles are correct for 1 to 8 targets, with the source Format excluded.
2. Hit-testing returns exactly one Slice or none, for the hub, the gaps and outside the ring (also checked by property tests).
3. A back Face exists only when more than 8 targets remain; otherwise it reports "no back Face" so the hint can be hidden.
4. For mixed files it returns the intersection of valid targets; an empty intersection returns a "split the Batch" result.
5. The library has no reference to UI or operating-system code, and its tests were written first.
