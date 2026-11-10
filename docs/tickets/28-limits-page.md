# 28 — Settings: Limits page

- **Status**: ready-for-agent
- **Size**: S
- **Blocked by**: [14a](14a-batch-engine.md), [24](24-settings-and-persistence.md)
- **Rules**: S2
- **Stories**: 42, 56 ([user stories](../spec/1.0-windows.md#user-stories))
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
The user can see and change the editable limits, within hard maxima.

## Acceptance criteria
1. Settings → Limits shows the editable limits of the Limits table with their defaults and hard maxima, and Reset to defaults.
2. Values above the hard maximum are rejected with a message; the bomb and complexity limits are not shown as editable.
3. A changed limit applies to the next Conversion.
4. The "limit reached" error message points to Settings → Limits.
