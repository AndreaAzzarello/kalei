# 11 — Donut library: geometry, hit-testing, Format intersection

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [05](05-solution-skeleton.md)
- **Rules**: none
- **Stories**: 20 ([user stories](../spec/1.0-windows.md#user-stories))
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
A UI-free library that decides everything about the Donut except drawing: where the Slices are, which one the cursor is over, and which Formats are valid for a mix of files.

## Acceptance criteria
1. Slice layout and angles are correct for 1 to 8 targets, with the source Format excluded, except TXT → TXT.
2. Hit-testing returns exactly one Slice or none, for the hub, the gaps and outside the ring (also checked by property tests).
3. More than 8 targets are not laid out and the library reports the case (no Module in 1.0 has it; the back Face comes with the first Module that does).
4. For several files it returns the Formats that every file can be converted to or already is, lists the files already in the chosen Format as skipped ("already in this format"), and returns a "split the Batch" result when the set is empty or leaves no file to convert.
5. The library has no reference to UI or operating-system code (dependency test of ticket [05](05-solution-skeleton.md)), and its tests were written first.
