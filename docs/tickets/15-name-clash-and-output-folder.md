# 15 — Name clash choice and Output folder choice

- **Status**: ready-for-agent
- **Size**: S
- **Blocked by**: [08](08-broker-and-output-writing.md), [10](10-main-window-single-file.md)
- **Rules**: S4
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
The user can choose where results go and, if they want, decide each name clash instead of the automatic rename.

## Acceptance criteria
1. The Output folder can be "next to the source" or a chosen folder with a picker.
2. "Ask every time" shows Keep both or Skip with "Apply to all"; Replace is never offered.
3. The choice is made before the Worker runs.
4. The default (automatic `name (1).ext`) is unchanged.
5. The dialog is fully keyboard and screen-reader operable.
