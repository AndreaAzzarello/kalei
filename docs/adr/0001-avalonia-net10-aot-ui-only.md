---
status: accepted (revised 2026-11-12: ReadyToRun + trimming ships; Native AOT is an optional trial, ticket 38)
---
# Avalonia 12 + .NET 10; ReadyToRun + trimming for the UI app, Native AOT as an optional trial

Kalei's UI uses Avalonia 12 on .NET 10. **Decision (2026-11-12): the UI app is published with ReadyToRun + trimming.** Native AOT was the first goal; it is not proven (the build spike could not link it on the dev PC, ticket 38 will try it on a GitHub Actions runner) and it is **no longer mandatory**: on the reference VM (2 vCPU, 4 GB, SATA SSD, Defender) ReadyToRun + trimming starts cold in about 970 ms (files already scanned) to about 1120 ms (first start after copy), under the revised W7 threshold of 1.3 s ([ticket-0](../ticket-0.md#w7--decision-after-ticket-03c-2026-11-12), ticket 03c). Native AOT stays an opportunistic trial after the CI (ticket 38), to be measured on the VM with the same kit against the numbers recorded there (baseline 968 ms, software rendering 811 ms); this ADR changes again only if it is adopted. Workers are regular self-contained .NET with trimming, not AOT, because many data libraries (XLSX, YAML) are not AOT-friendly and start-up time matters little there. Original text, kept for the record: Avalonia documents Native AOT, but Filee (same stack) does not use it (its project files use ReadyToRun) and notes that linking SkiaSharp's native libraries is a problem for its installer.

## Considered options

- **WPF**: reuses the experience and C#/XAML patterns from Baton, the author's earlier app (a download manager in C#/WPF/.NET 10 with a browser extension, repository `AndreaAzzarello/baton`; Avalonia is close to WPF), but Windows-only and no AOT/trimming, so a bigger installer; it also contradicts the multiplatform architecture of [ADR 0010](0010-multiplatform-architecture-windows-first.md).
- **Tauri 2**: smallest installer (~5–10 MB, system WebView2), but adds Rust and a web frontend on the security-critical path (engines, sandbox) and extra WebView processes.

## Consequences

- Core stays UI-independent pure .NET with no OS API (see [ADR 0010](0010-multiplatform-architecture-windows-first.md)), so macOS and Linux remain possible; Avalonia itself runs on all three.
- AOT/trimming compatibility of every library must be checked and recorded in the TRD; trimming is what ships, AOT compatibility is kept (`IsAotCompatible`) so the trial stays possible.
- Start-up is watched against the 1.3 s threshold of W7; the embedded font and the real window (ticket 10a) are measured again there.
