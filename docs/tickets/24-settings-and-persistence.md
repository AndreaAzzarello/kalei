# 24 — Settings and persistence

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [10](10-main-window-single-file.md)
- **Rules**: S10
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
The user's choices (language, theme, colour transition, output folder, name clash, defaults) are kept safely between runs, installed or portable.

## Acceptance criteria
1. The settings file follows the settings spec: atomic writes, unknown keys kept, migration by version.
2. A corrupt file gives defaults, a single `.bad` copy and a short notice.
3. Language, theme, colour transition, output folder and name clash can be changed in Settings and take effect.
4. With the portable marker the data lives next to the executable; if that is not writable, Kalei says so and runs with defaults.
5. The chosen folder path never reaches a log (S10).
