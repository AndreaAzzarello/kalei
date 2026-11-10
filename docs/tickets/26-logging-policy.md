# 26 — Logging: format, rotation and the no-paths test

- **Status**: ready-for-agent
- **Size**: S
- **Blocked by**: [05](05-solution-skeleton.md)
- **Rules**: S10
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
The app writes useful local logs that can be shared safely, because they never contain a path or file name.

## Acceptance criteria
1. Logs are JSON lines with timestamp, level, event code, duration, Format identifiers, Module id and version, error code.
2. Rotation keeps at most 5 files of 1 MB.
3. Open log folder opens the log folder.
4. A test runs conversions with distinctive sample paths and names and scans every log line; it fails if any of them or any path-like text appears.
5. No telemetry or network call exists.
