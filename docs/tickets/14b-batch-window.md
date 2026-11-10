# 14b — Batch window: list, status, Cancel all, Retry failed, mixed files

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [14a](14a-batch-engine.md), [10b](10b-main-window-convert.md), [12b](12b-in-app-donut-host.md)
- **Rules**: S2, S4
- **Stories**: 21, 23, 24, 25, 28, 29 ([user stories](../spec/1.0-windows.md#user-stories))
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
The user drops many files or a whole folder, sees one status per file and converts them in one go with a single target Format.

## Acceptance criteria
1. Several files and folders can be dropped; the Batch state lists each file with its status (queued, converting, done, error, skipped with its reason) and shows overall progress.
2. With 10,000 rows the window stays responsive: the UI thread is never blocked for more than 100 ms (frame-time probe) on the main reference machine (initial threshold; virtualised list).
3. The Batch has one target Format; Cancel all and Retry failed buttons work.
4. With mixed files the target list and the Donut offer the Format rule set; with none, "These files have no format in common. Convert them in separate groups." appears, the Donut does not open and nothing starts.
