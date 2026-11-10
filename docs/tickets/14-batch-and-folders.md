# 14 — Batch and dropped folders

- **Status**: ready-for-agent
- **Size**: L
- **Blocked by**: [07](07-launcher-and-worker-ipc.md), [10](10-main-window-single-file.md)
- **Rules**: S2, S4
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
The user drops many files or a whole folder and converts them in one go, with a single target Format, and sees what happened to each file.

## Acceptance criteria
1. Several files and folders can be dropped; folders are walked to depth 32; with a chosen Output folder the folder structure is kept.
2. Links and junctions are not followed; hidden and system files are skipped; every skipped file shows its reason.
3. The Batch has one target Format; for mixed Categories the valid intersection is offered; with none, "These files have no format in common. Convert them in separate groups." appears and nothing starts.
4. Parallel Workers follow the Limits formula; one failing file never stops the others.
5. Overall progress, per-file status, Cancel all and Retry failed work.
6. 10,000 files keep the window responsive (virtualised list, measured on the reference machine); beyond the limit E_COUNT is shown.
