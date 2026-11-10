# Kalei 1.0 — Build and release pipeline

Part of the [1.0 spec](1.0-windows.md). Rules it serves: S1, S5 (sandbox set-up), S6 (no automatic update), and the licence rules in [ADR 0005](../adr/0005-mit-license-copyleft-engines-as-separate-processes.md). Publishing a release always needs the owner's explicit confirmation.

## Target
Windows 10 22H2 (build 19045) and Windows 11, x64. No ARM64 build in 1.0. Per-user installer, no administrator rights; portable ZIP.

## Pipeline, in order
1. **Restore, build, test, format check** (`dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`). Any failure stops the pipeline.
2. **Publish the app** with the build mode Ticket 0 chose (Native AOT, or ReadyToRun and trimming if W7 fails).
3. **Publish the Worker executable** self-contained and trimmed, with warnings as errors (W9).
4. **Third-party notices**: generate THIRD-PARTY-NOTICES from the dependency list and the licence texts (libvips and other LGPL notices with the source offer, OFL text for the font, MIT/BSD/Apache texts); the About page shows it. A missing licence text fails the build.
5. **Portable ZIP**: the app, the Worker, notices and the `kalei.portable` marker file.
6. **Installer** (Inno Setup, `PrivilegesRequired=lowest`): installs to the user's programs folder, creates the Start menu entry, and sets the sandbox access described below.
7. **`SHA256SUMS`** for the installer and the ZIP.
8. **Signing**, only if W18 confirmed that SignPath Foundation accepts the project: sign the installer and the executables before step 7. Without it, 1.0 ships unsigned and the README says so (SmartScreen will warn; the sums let a user verify the download).
9. **Draft release** on GitHub Releases with the files and sums. Publishing the release is a separate step, only after the owner confirms.

## Sandbox set-up at install time
- The installer grants read and execute on the install folder to ALL APPLICATION PACKAGES, so an AppContainer Worker can load its own files (W4).
- The AppContainer profiles (one per Module) are created by the app at first run (not by the installer, which runs without administrator rights) and removed on uninstall through an app command the uninstaller calls.
- **Portable ZIP**: the app sets the same access on its own folder at first run if it can; if it cannot (read-only folder, or a location the user does not own), it shows a clear message and does not convert until the sandbox can start. It never converts without the sandbox.

## Uninstall
Removes the program files, the Start menu entry, the AppContainer profiles and the temporary folder. **Keeps** `settings.json` unless the user ticks "Also delete my settings and logs".

## CI and cost
GitHub Actions on a public repository, Windows runner. **To confirm at repository set-up that the free plan covers it** before anything depends on it; if it does not, stop and ask (no card on file, free services only). Until then the same steps run locally in PowerShell. No secret is needed for steps 1–7; signing (step 8) would need SignPath's own credentials, kept as repository secrets and never in the repository.

## From 1.1 (not in 1.0)
When the first Optional module exists the order becomes: build the module → hash every file → embed the hashes in the app → build the app → build the `.kmod` and the installer ([ADR 0004](../adr/0004-optional-modules-embedded-hashes.md), S8). Each module update is therefore a new app release.

## Open checks before the first release
Inno Setup licence text for the version used (W18), SignPath eligibility (W18), free-plan check for CI, installer size against the 45 MB target and 60 MB limit (W8).
