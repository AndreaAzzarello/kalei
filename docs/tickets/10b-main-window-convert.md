# 10b — Main window: Convert, progress, Cancel, Done and Error

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [10a](10a-main-window-file-card.md)
- **Rules**: S3, S4, S10
- **Stories**: 8, 9, 10, 11, 40, 42 ([user stories](../spec/1.0-windows.md#user-stories))
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
The user converts the file, sees progress, can cancel, and gets the result or a clear error.

## Acceptance criteria
1. Convert starts the Conversion; the progress bar updates at least once per second, and when the Worker reports no fraction an indeterminate bar is shown.
2. Cancel asks the Worker to stop (killed after 3 s) and removes the partial Output file; the window returns to the ready state.
3. Done shows the output name, the real size, Show in folder and Convert another; the Source file is unchanged.
4. Errors show a plain-language message and a short code from the catalogue, never a path; Retry works.
5. A limit error names the limit and points to Settings → Limits.
