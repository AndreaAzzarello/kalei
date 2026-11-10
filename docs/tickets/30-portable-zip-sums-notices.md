# 30 — Portable ZIP, SHA256SUMS, third-party notices and About

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [03](03-build-and-size-spike.md), [10](10-main-window-single-file.md)
- **Rules**: S6
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
A reproducible build produces a portable ZIP, checksums and the licence notices, and the app shows them.

## Acceptance criteria
1. The pipeline steps of the release spec up to the ZIP and SHA256SUMS run locally in PowerShell and fail on any build, test or format error.
2. The ZIP contains the app, the Worker, the notices and the portable marker.
3. THIRD-PARTY-NOTICES is generated from the dependency list with every licence text; a missing licence text fails the build.
4. About shows the notices; "Check for updates" only opens the Releases page (S6).
5. The README explains how to verify the download with the sums.
