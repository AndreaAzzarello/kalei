# 39 — CI on GitHub Actions: build, test, format and trimming checks on every push and PR

- **Status**: ready-for-agent
- **Size**: S
- **Blocked by**: [05](05-solution-skeleton.md)
- **Rules**: S6, S10
- **Stories**: none (constraint, spike or infrastructure)
- **Spec**: [Release pipeline](../spec/release-pipeline.md)

## What to build
A continuous-integration workflow on GitHub Actions that runs, on every push and every pull request, the checks that the project already requires by hand: build, tests, format check and the trimming-warning check of the Worker. It uses only free standard runners of a public repository and holds no secret. Why a separate ticket: no other ticket creates a GitHub Actions workflow (the pipeline of #37 is local PowerShell), and the Native AOT trial (ticket [38](38-native-aot-trial.md)) and the release ticket (ticket [30](30-portable-zip-sums-notices.md)) need one to run on.

## Acceptance criteria
1. A workflow runs on `push` and on `pull_request` on a `windows-latest` standard runner. The billing page for free standard runners on public repositories is checked first and the result written in HISTORY; if the repository is private or the minutes are not free, stop and ask.
2. The steps are: restore, `dotnet build -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`. A failing step fails the run.
3. The trimming check: the Worker is published trimmed (self-contained, win-x64) with trimming warnings as errors, as in W9 of ticket-0, and the smoke test runs on the trimmed binary. A new trimming warning fails the run.
4. Every action is pinned to a full commit SHA (the version in a comment), never a tag or branch. Permissions are `contents: read` at workflow level and nothing more; no secret is used; no `pull_request_target`; the workflow never runs code from a fork with write access. Cookies, analytics and uploads to third-party services are not used (artefacts only if needed, with a short retention).
5. The .NET SDK is pinned to 10.0.14 or later (a `global.json` and the setup step), because of advisory GHSA-p3jp-9v83-76w9 (runtime and ILCompiler below 10.0.14). The workflow fails if a restored runtime or ILCompiler package is below 10.0.14, and the NuGet audit reports no high advisory that is not analysed in the TRD.
6. Measured and written in HISTORY: duration of a full run, and the minutes used per run.
7. The branch protection or the repository setting that requires this check before a merge is a recommendation in the README of the ticket folder, not set by the agent (the owner decides).
