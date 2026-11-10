# 33 — Final verification: the S1–S12 gate and a release dry run

- **Status**: needs-owner-hardware
- **Size**: L
- **Blocked by**: [13](13-probe-and-preview.md), [15](15-name-clash-and-output-folder.md), [16](16-images-avif-gif.md), [17](17-images-bmp-ico.md), [18](18-images-heic-via-wic.md), [22](22-json-tabular.md), [23](23-text-module.md), [25](25-localisation-and-error-catalogue.md), [26](26-logging-policy.md), [27](27-browser-drops-and-origin-mark.md), [28](28-limits-page.md), [29](29-accessibility-pass.md), [32](32-code-signing.md)
- **Rules**: S1–S12
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
A proof, not a promise: every requirement is checked against the installed build and a draft release is produced, so the owner can decide to publish.

## Acceptance criteria
1. The S1–S12 checklist has a test or a recorded manual proof per item; S8 and S11 are marked not applicable to 1.0.
2. The network gate (TCP and DNS from a Worker must fail) passes on the installed build on the Hyper-V Windows 10 VM and on the Windows 11 PC; otherwise the promise is removed (S5).
3. The sample set for every Format (valid, truncated, wrong extension, generated huge, malicious archive or XLSX) fails or converts cleanly with no temporary file left.
4. The log scan finds no path or file name; the installed build makes no network connection.
5. A dry run produces the draft release files and the sums verify.
6. A written report lists every unverified item; publishing needs the owner's explicit confirmation.
