# 24 — Settings and persistence

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [10b](10b-main-window-convert.md)
- **Rules**: S6, S10
- **Stories**: 35, 53, 54, 55, 57 ([user stories](../spec/1.0-windows.md#user-stories))
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
The user's choices (language, theme, colour transition, output folder, name clash) are kept safely between runs, installed or portable.

## Acceptance criteria
1. The settings file follows the settings spec: atomic writes, unknown keys kept, migration by version.
2. A corrupt file gives defaults, a single .bad copy and a short notice.
3. Language, theme, Colour transition, output folder and name clash can be changed in Settings and take effect.
4. With the portable marker the data lives next to the executable; if that folder is not writable, Kalei says so and does not convert.
5. Open log folder opens the logs folder; Open Releases page opens the project's GitHub Releases page in the default browser and is the only action of Kalei that touches the network (the browser does it, Kalei makes no connection).
6. The chosen folder path never reaches a log (S10). Per-Conversion options are not saved between runs.
