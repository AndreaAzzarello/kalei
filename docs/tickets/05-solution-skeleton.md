# 05 — Solution skeleton: projects, platform adapter interfaces, quality gates

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [03](03-build-and-size-spike.md)
- **Rules**: S10
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
A solution that builds, tests and format-checks cleanly, with the project layout of the architecture and the seven platform adapter interfaces, so every later ticket has a place to land.

## Acceptance criteria
1. The solution has projects for the app, the Core, the Donut library, the Worker host, the Windows adapters and the tests; the build mode is the one chosen by ticket 03.
2. The seven platform adapter interfaces exist in the Core (sandbox launcher, image codec, global pointer hook, paths, shell integration, file origin, installer layout); the Windows implementations live in a separate project.
3. A test fails if the Core references any operating-system API or the Windows adapter project.
4. Build, test and the format check all pass (`dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`).
5. A documented location for test samples exists (large samples are generated, not committed).
6. A logging abstraction exists that cannot be given a path or file name (S10).
