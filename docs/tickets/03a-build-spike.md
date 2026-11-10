# 03a — Build spike: Native AOT, trimmed Worker, CSV library, first size estimate

- **Status**: needs-owner-hardware
- **Size**: M
- **Blocked by**: –
- **Rules**: none
- **Stories**: none (constraint, spike or infrastructure)
- **Proofs**: W7, W9, W8 (first estimate) ([ticket-0](../ticket-0.md))
- **Spec**: [ticket-0, Part A](../ticket-0.md#part-a--windows-blocks-10)

## What to build
Decide how the app and the Worker are built, with numbers from the main reference machine: Native AOT or ReadyToRun, trimming, the CSV library, and a first estimate of the download size. This ticket unblocks the solution skeleton.

## Acceptance criteria
1. W7: a Native AOT publish of an Avalonia + Skia window succeeds and the cold start, measured from process start to the first frame drawn with the window accepting input (median of 10 cold runs, see Timings in ticket-0), is 1 s or less on the main reference machine; otherwise ReadyToRun with trimming is chosen and ADR 0001 is updated.
2. W9: the Worker publishes trimmed with warnings as errors using the 1.0 libraries; a library that fails is replaced; the CSV library (Sep or CsvHelper) is decided and the TRD updated.
3. W8, first estimate: the compressed size of the published app plus Worker is measured and compared with the target (45 MB) and the limit (60 MB); the final measure on the real installer is in ticket [31](31-per-user-installer.md).
   - **Status (2026-11-10)**: Native AOT could not be tried on the dev PC (no MSVC linker; owner decision: do not install the Build Tools). ReadyToRun + trimming was measured as the fallback. The AOT trial moves to ticket [38](38-native-aot-trial.md) (GitHub Actions Windows runner, advisory analysis first); ADR 0001 is updated only after its outcome. The cold-start measure on the Hyper-V VM is in issue #47. This ticket closes on W9, W8 and the fallback measure.
4. All results are written in HISTORY, and the TRD is updated; ADR 0001 stays as it is until ticket 38.
