# 03 — Build spike: Native AOT, trimmed Worker, installer size, antivirus, licences

- **Status**: needs-owner-hardware
- **Size**: L
- **Blocked by**: none
- **Rules**: none
- **Proofs**: W7, W8, W9, W17, W18 ([ticket-0](../ticket-0.md))
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
Decide how the app and the Worker are built and shipped, with numbers from the reference machine: Native AOT or ReadyToRun, trimming, size, antivirus behaviour, and the licence checks that gate the installer and signing.

## Acceptance criteria
1. W7: Native AOT publish of an Avalonia + Skia window succeeds and cold start is 1 s or less on the Hyper-V reference machine; otherwise ReadyToRun with trimming is chosen and ADR 0001 is updated.
2. W9: the Worker publishes trimmed with warnings as errors using the 1.0 libraries; libraries that fail are replaced; the CSV library (Sep or CsvHelper) is decided.
3. W8: the size of a per-user installer and of the portable ZIP is measured: target 45 MB or less, acceptable 60 MB, above that the scope is reviewed with the owner.
4. W17: Microsoft Defender, one other antivirus and SmartScreen results for the installer and the app are recorded.
5. W18: the Inno Setup licence text for the version used and the SignPath Foundation eligibility terms are read and summarised; if either is unsuitable, an alternative is proposed to the owner.
6. All results are written in the history, and ADR 0001, the TRD and the release pipeline spec are updated.
