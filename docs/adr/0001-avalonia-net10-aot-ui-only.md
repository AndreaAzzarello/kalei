---
status: accepted (Native AOT part to be verified in Ticket 0)
---
# Avalonia 12 + .NET 10; Native AOT for the UI app only

Kalei's UI uses Avalonia 12 on .NET 10, published with Native AOT for fast start and a small footprint. **Native AOT is a goal, not yet proven:** Avalonia documents it, but Filee (same stack) does not use it (its project files use ReadyToRun) and notes that linking SkiaSharp's native libraries is a problem for its installer. Ticket 0 checks Native AOT with Avalonia + Skia; if it does not hold, the fallback is ReadyToRun + trimming and this ADR is updated. Workers are regular self-contained .NET with trimming, not AOT, because many data libraries (XLSX, YAML) are not AOT-friendly and start-up time matters little there.

## Considered options

- **WPF**: reuses the experience and C#/XAML patterns from Baton, the author's earlier app (a download manager in C#/WPF/.NET 10 with a browser extension, repository `AndreaAzzarello/baton`; Avalonia is close to WPF), but Windows-only and no AOT/trimming, so a bigger installer; it also contradicts the multiplatform architecture of [ADR 0010](0010-multiplatform-architecture-windows-first.md).
- **Tauri 2**: smallest installer (~5–10 MB, system WebView2), but adds Rust and a web frontend on the security-critical path (engines, sandbox) and extra WebView processes.

## Consequences

- Core stays UI-independent pure .NET with no OS API (see [ADR 0010](0010-multiplatform-architecture-windows-first.md)), so macOS and Linux remain possible; Avalonia itself runs on all three.
- AOT/trimming compatibility of every library must be checked and recorded in the TRD.
