# 33 — Final verification: the S1–S12 gate and a release dry run

- **Status**: needs-owner-hardware
- **Size**: L
- **Blocked by**: [13](13-probe-and-preview.md), [14b](14b-batch-window.md), [16](16-images-avif-gif.md), [17](17-images-bmp-ico.md), [18](18-images-heic-via-wic.md), [22](22-json-tabular.md), [23](23-text-module.md), [25](25-localisation-and-error-catalogue.md), [26](26-logging-policy.md), [27](27-browser-drops-and-origin-mark.md), [29](29-accessibility-pass.md), [32](32-code-signing-and-final-sums.md), [37](37-advanced-panel.md)
- **Rules**: S1–S12
- **Stories**: 43 ([user stories](../spec/1.0-windows.md#user-stories))
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
A proof, not a promise: every requirement is checked against the installed build and a draft release is produced, so the owner can decide to publish.

## Acceptance criteria
1. A table in the verification file for 1.0 lists S1–S12, each with its test or recorded manual proof, the outcome and the date; the manifest-rejection part of S8 passes and its hash part is marked not applicable to 1.0; S11 is marked not applicable.
2. The network gate (a TCP connect and a DNS lookup from a Worker must both fail) passes on the installed build on the Hyper-V Windows 10 VM and on the Windows 11 PC; otherwise the promise is removed (S5).
3. The sample set for every Format (valid, truncated, wrong extension, generated huge, malicious archive or XLSX) fails or converts cleanly with no temporary file left.
4. The log scan finds no path or file name; the static check finds no networking reference in the app and Core; while a complete scenario runs for at least 2 minutes on the VM with DNS blocked, the process's TCP and UDP connection list stays empty (checked once per second).
5. A dry run produces the draft release files and the sums verify.
6. A written report lists every unverified item; publishing needs the owner's explicit confirmation.
