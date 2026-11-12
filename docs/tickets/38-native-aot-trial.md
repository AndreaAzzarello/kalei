# 38 — Native AOT trial on a GitHub Actions Windows runner

- **Status**: ready-for-agent
- **Size**: S
- **Blocked by**: [05](05-solution-skeleton.md), [30](30-portable-zip-sums-notices.md), [39](39-ci-github-actions.md)
- **Rules**: none
- **Stories**: none (constraint, spike or infrastructure)
- **Proofs**: W7, Native AOT part ([ticket-0](../ticket-0.md))
- **Spec**: [ADR 0001](../adr/0001-avalonia-net10-aot-ui-only.md)

## What to build
The build spike (#3) could not try Native AOT: the dev PC has no MSVC linker and the owner did not install the Build Tools. This ticket tries it on a GitHub Actions Windows runner, which has the toolchain, and only then decides ADR 0001. The CI workflow comes from ticket [39](39-ci-github-actions.md); this ticket adds a manual-dispatch job to it.

## Acceptance criteria
1. First, the advisory GHSA-p3jp-9v83-76w9 (CVE-2026-111497, Brotli decoder out-of-bounds write, high, 8.1) is analysed before AOT is adopted. Facts already read: it lists `Microsoft.DotNet.ILCompiler`, `Microsoft.NETCore.App.Runtime.NativeAOT.win-x64` and `Microsoft.NETCore.App.Runtime.win-x64` below 10.0.14 (patched in 10.0.14); the dev PC and the spike binaries use 10.0.13. It affects an app only when it uses Brotli decompression on attacker-controlled data. To record in the TRD: whether Kalei uses any Brotli decompression (expected no: no HttpClient, no network), whether the release build pins runtime and ILCompiler to 10.0.14 or later, and that the audit warning is gone.
2. A job with manual dispatch, added to the workflow of ticket 39, publishes the Avalonia window of the spike (branch `spike/03a-build`, or the app skeleton once it exists) with `PublishAot=true` on a `windows-latest` runner; the runner image and the MSVC version are recorded. The billing page for free standard runners on public repositories is checked first (release-pipeline spec).
3. Outcome recorded: publish succeeds or fails (with the log's cause), size of the published folder and ZIP, warm start-up on the runner, compared with ReadyToRun + trimming from #3 (median 418 ms warm, 23.5 MB zipped on the dev PC). Runner timings are not the VM's: the cold start on the VM stays in #47.
4. ADR 0001 is updated only if AOT is adopted after this outcome and the VM cold start; ReadyToRun + trimming is already the decided build (2026-11-12) and stays if AOT does not clearly win.
   - **Context (2026-11-11)**: ReadyToRun + trimming failed W7 on the reference VM (median 1122.7 ms against 1000 ms). AOT is one of three ways out, with a revised threshold and optimisation without AOT; ticket [03c](03c-w7-cold-start-breakdown.md) measures where the time goes first, and AOT is also to be measured on the VM, not only on the runner.
   - **Decision (2026-11-12)**: AOT is **not mandatory**. The cold-start threshold is now 1.3 s and ReadyToRun + trimming meets it ([ticket-0](../ticket-0.md#w7--decision-after-ticket-03c-2026-11-12)); this ticket stays an opportunistic trial after the CI of ticket [39](39-ci-github-actions.md). Numbers to beat on the reference VM (median of cold starts, second frame, ticket [03c](03c-w7-cold-start-breakdown.md)): ReadyToRun + trimming 968 ms; with software rendering 811 ms. The VM run uses the same kit as 03c (branch `spike/03a-build`, `w7-kit`), **without the automatic sign-in** (the checkpoint "aggiornato" now has no password).
