# 28 — Settings: Limits page

- **Status**: ready-for-agent
- **Size**: S
- **Blocked by**: [14](14-batch-and-folders.md), [24](24-settings-and-persistence.md)
- **Rules**: S2
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
The user can see and change the editable limits, within hard bounds.

## Acceptance criteria
1. Settings → Limits shows the editable limits with their defaults and hard upper bounds, and Reset to defaults.
2. Bomb and complexity limits are not editable.
3. A changed limit applies to the next Conversion; invalid values are rejected with a message.
4. The "limit reached" error message points to Settings → Limits.
