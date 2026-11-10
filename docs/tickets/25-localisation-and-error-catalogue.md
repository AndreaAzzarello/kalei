# 25 — Localisation (English and Italian) and the error catalogue

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [10](10-main-window-single-file.md)
- **Rules**: S10
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
Every word in the app exists in English and Italian, and every error has a plain message in both.

## Acceptance criteria
1. All UI strings come from keyed resources in English and Italian; the system language is used by default, English if it is neither.
2. A test fails if any key is missing in either language, or if an error code has no message.
3. No message contains a path or file name.
4. The mechanism works in the trimmed, published build.
5. Number, date and size formats follow the chosen language.
