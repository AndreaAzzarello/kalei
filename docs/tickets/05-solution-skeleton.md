# 05 — Solution skeleton: projects, adapter interfaces, paths adapter, quality gates

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [03a](03a-build-spike.md)
- **Rules**: S3, S6, S10
- **Stories**: none (constraint, spike or infrastructure)
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
A solution that builds, tests and format-checks cleanly, with the project layout of the architecture, the platform adapter interfaces that 1.0 uses and the Windows paths adapter, so every later ticket has a place to land.

## Acceptance criteria
1. The solution has projects for the app, the Core, the Donut library, the Worker host, the Windows adapters and the tests, using the build mode chosen by ticket [03a](03a-build-spike.md).
2. The interfaces of the adapters used in 1.0 exist in the Core (sandbox launcher, system image codec, paths, file origin, installer layout); the Windows implementations live in a separate project. The pointer-hook and shell-integration interfaces (ADR 0010) are not created until the tickets that implement them (34 and 1.1).
3. The Windows paths adapter returns the data, log, settings and temporary folders for the installed and the portable mode (marker file), and reports a data folder that is not writable; tests cover both modes.
4. A dependency test fails if the Core references any of System.Net.*, System.Diagnostics.Process, System.IO.Pipes, Microsoft.Win32.* or declares a native call, if the app references System.Net.*, or if the Donut library references any UI or operating-system API.
5. Build, test and the format check all pass (dotnet build, dotnet test, dotnet format --verify-no-changes).
6. A documented location for test samples exists (large samples are generated, not committed).
7. A logging abstraction exists that cannot be given a path or file name (S10).
