# 25 — Localisation completeness and the message catalogue

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [10b](10b-main-window-convert.md), [14b](14b-batch-window.md), [15](15-name-clash-and-output-folder.md), [24](24-settings-and-persistence.md), [27](27-browser-drops-and-origin-mark.md), [28](28-limits-page.md)
- **Rules**: S10
- **Stories**: 40, 53 ([user stories](../spec/1.0-windows.md#user-stories))
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
Every word in the app exists in English and Italian, and every error and notice has a plain message in both.

## Acceptance criteria
1. Every UI string is a keyed resource with English and Italian text; the system language is used by default, English if it is neither.
2. A test fails if a key is missing in either language, or if an error code or notice of the IPC spec has no message.
3. No message contains a path, a file name or a placeholder for one.
4. Number, date and size formats follow the chosen language.
5. The owner has reviewed the Italian text (recorded in HISTORY).
