# 30 — Portable ZIP, provisional SHA256SUMS, third-party notices and About

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [03a](03a-build-spike.md), [10a](10a-main-window-file-card.md)
- **Rules**: S6
- **Stories**: 62, 63, 64 ([user stories](../spec/1.0-windows.md#user-stories))
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
A reproducible build produces a portable ZIP, a first checksum file and the licence notices, and the app shows the notices.

## Acceptance criteria
1. Steps 1 to 4 and 6 of the release pipeline run locally in PowerShell and stop on any build, test or format error; the signing steps do nothing until ticket [32](32-code-signing-and-final-sums.md).
2. The ZIP contains the app, the Worker, the notices and the portable marker.
3. THIRD-PARTY-NOTICES is generated from the dependency list with every licence text; a missing licence text fails the build.
4. About shows the notices.
5. A provisional SHA256SUMS for the unsigned ZIP is produced; the README explains how to verify a download and how to run the clean-up command before deleting a portable folder.
