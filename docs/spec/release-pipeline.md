# Kalei 1.0 — Build and release pipeline

Part of the [1.0 spec](1.0-windows.md). Rules it serves: S1, S5 (sandbox set-up), S6 (no automatic update), and the licence rules in [ADR 0005](../adr/0005-mit-license-copyleft-engines-as-separate-processes.md). Publishing a release always needs the owner's explicit confirmation.

## Target
Windows 10 22H2 (build 19045) and Windows 11, x64. No ARM64 build in 1.0. Per-user installer, no administrator rights; portable ZIP.

## Pipeline, in order
1. **Restore, build, test, format check** (`dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`). Any failure stops the pipeline.
2. **Publish the app** with the build mode the build spike chose (Native AOT, or ReadyToRun and trimming if W7 fails).
3. **Publish the Worker executable** self-contained and trimmed, with warnings as errors (W9).
4. **Third-party notices**: generate THIRD-PARTY-NOTICES from the dependency list and the licence texts (libvips and other LGPL notices with the source offer, the OFL text for the font once the original licence text of Mayenne Sans is obtained (TRD to-do), MIT/BSD/Apache texts); the About page shows it. A missing licence text fails the build.
5. **Sign the executables**, only if W18 confirmed that SignPath Foundation accepts the project.
6. **Portable ZIP**: the app, the Worker (signed if step 5 ran), notices and the `kalei.portable` marker file.
7. **Installer** (Inno Setup, `PrivilegesRequired=lowest`): installs to the user's programs folder, creates the Start menu entry, and sets the sandbox access described below.
8. **Sign the installer**, same condition as step 5. Without signing, 1.0 ships unsigned and the README says so (SmartScreen will warn; the sums let a user verify the download).
9. **`SHA256SUMS`** for the final installer and ZIP, always the last step before the release so that the sums match the signed files.
10. **Draft release** on GitHub Releases with the files and sums. Publishing the release is a separate step, only after the owner confirms.

Tickets: 30 builds steps 1–4 and 6 and a provisional sums file without signing; 31 adds the installer; 32 adds signing and makes the final sums.

## Sandbox set-up at install time
- The installer grants read and execute on the install folder to ALL APPLICATION PACKAGES, so an AppContainer Worker can load its own files (W4).
- The AppContainer profiles (one per Worker executable; in 1.0 one, for the built-in Worker) are created by the app at first run (not by the installer, which runs without administrator rights) and removed on uninstall through an app command the uninstaller calls.
- **Portable ZIP**: the app sets the same access on its own folder at first run if it can; if it cannot (read-only folder, or a location the user does not own), it shows a clear message and does not convert until the sandbox can start. It never converts without the sandbox. A copied portable folder leaves the AppContainer profile and the folder access behind: the app has a clean-up command (the uninstaller calls the same one), and the README says how to run it before deleting a portable folder.

## Uninstall
Removes the program files, the Start menu entry, the AppContainer profile, the journal and the temporary folder. **Keeps** `settings.json` and the logs folder unless the user ticks "Also delete my settings and logs", which then removes the whole data folder.

## CI and cost
GitHub Actions on a public repository, Windows runner. **Verified 2026-11-10** on the primary source (https://docs.github.com/en/billing/concepts/product-billing/github-actions): "Standard GitHub-hosted runners in public repositories" are listed as free usage; larger runners are billed for public repositories and are never used here. The page does not mention Windows runners separately, so the first workflow run must be checked on the billing page (nothing charged); if anything is charged, stop and ask (no card on file, free services only). The same steps run locally in PowerShell. No secret is needed for steps 1–4, 6, 7, 9 and 10; signing (steps 5 and 8) would need SignPath's own credentials, kept as repository secrets and never in the repository.

## Out of Scope: from 1.1 (not in 1.0)
When the first Optional module exists the order becomes: build the module → hash every file → embed the hashes in the app → build the app → build the `.kmod` and the installer ([ADR 0004](../adr/0004-optional-modules-embedded-hashes.md), S8). Each module update is therefore a new app release.

## Testing and further notes: open checks before the first release
Inno Setup licence text and SignPath eligibility (W18, ticket 03b), the first workflow run checked on the billing page, installer size against the 45 MB target and 60 MB limit (W8, ticket 31).

The pipeline is tested by running it (tickets 30, 31, 32) and by the dry run of ticket 33.
