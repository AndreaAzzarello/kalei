# 26 — Logging: format, rotation and the no-paths test

- **Status**: ready-for-agent
- **Size**: S
- **Blocked by**: [05](05-solution-skeleton.md), [09a](09a-images-module-core.md)
- **Rules**: S6, S10
- **Stories**: 46 ([user stories](../spec/1.0-windows.md#user-stories))
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
The app writes useful local logs that can be shared safely, because they never contain a path or file name.

## Acceptance criteria
1. Logs are JSON lines with timestamp, level, event code, duration, Format identifiers, Module id and version, error code; the levels are error and warning only.
2. Rotation keeps at most 5 files of 1 MB.
3. A test runs real conversions, successful and failing, with distinctive sample paths and names, and scans every log line; it fails if any of them or any path-like text appears.
4. The logging code has no network reference (dependency test of ticket [05](05-solution-skeleton.md)); there is no telemetry.
