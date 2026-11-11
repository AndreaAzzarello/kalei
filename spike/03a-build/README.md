# Spike 03a: build (W7, W8 first estimate, W9)

Throwaway code for ticket 03a (issue #3). It lives only on the branch `spike/03a-build` and is **never merged into main**; only the measured results go back into the documents.

- `AotWindow/`: minimal Avalonia 12.1.6 + Skia window (one AXAML view, compiled bindings on). It measures its own start-up (process creation to the second animation frame after the window opened) and, with `--out file`, writes it and exits.
  - Native AOT: `dotnet publish AotWindow -c Release -r win-x64 -p:PublishAot=true -o artifacts/aot` (needs the MSVC linker: "Desktop development with C++").
  - Fallback: `dotnet publish AotWindow -c Release -r win-x64 --self-contained -p:PublishReadyToRun=true -p:PublishTrimmed=true -o artifacts/r2r`.
  - `measure.ps1` runs 10 start-ups and measures folder and ZIP size without `.pdb`.
- `TrimmedWorker/`: self-contained trimmed Worker with every 1.0 library and a smoke test that runs on the trimmed binary (trimming can break reflection without any warning). Publish: `dotnet publish TrimmedWorker -c Release -o artifacts/worker-b` (warnings are errors).
- `results/`: sanitised outputs. Run A = libraries as listed in the TRD (YamlDotNet typed API, MiniExcel); run B = YamlStream + Sylvan.Data.Excel reader + own XLSX writer, with warnings as errors.

Timings are warm (file cache not dropped, no reboot): not the cold timings asked by ticket-0.

## Ticket 03c (issue #56): W7 cold start, where the time goes

W7 failed on the reference VM with ReadyToRun + trimming (median 1122.7 ms against 1000 ms). The window now writes phase markers and there are six variants to compare, all self-contained, ReadyToRun + trimming. Nothing here is product code; the branch is never merged.

- **Markers** (`StartupMarks.cs`, ms from the kernel creation time of the process; written as one JSON line with `--out file`; no path, user or computer name): `runtime` (first managed code, module initializer: includes apphost, hostfxr, hostpolicy and CLR start, not separable without event tracing), `main`, `builderReady`, `platformServices` (Avalonia platform and Skia set up), `appInitStart`, `appInitEnd`, `afterSetup`, `frameworkReady`, `windowCtorEnd`, `firstLayout`, `opened`, `frame1` (start of the first frame after open), `frame2` (the W7 metric of #47, unchanged), `fullContent` (only b, c, f: the frame that shows the complete content; the other variants have it at `frame2`). After `frame2` it also records the loaded assembly names, CPU user/kernel, bytes read with ReadFile (GetProcessIoCounters: memory-mapped image loads are NOT counted, so this is not disk traffic) and peak working set.
- **Variants** (`build-variants.ps1`, `-p:Variant=x`):
  - **a** baseline of #47 (plus the markers; the marks cost a few microseconds).
  - **b** window shown at once with logotype and border built in code; the full view (`FullView.axaml`) is loaded after `frame2`. Report both `frame2` and `fullContent`: the minimal window is not the finished window.
  - **c** lean start: `Avalonia.Desktop` removed, `UseWin32().UseSkia().UseHarfBuzz()` instead of `UsePlatformDetect()`, explicit default font (`FontManagerOptions.DefaultFamilyName = "Segoe UI"`), ComboBox filled after `frame2`. Assemblies removed: only `Avalonia.Desktop.dll` (27 loaded at `frame2` become 26). The baseline never loaded X11, FreeDesktop or Native: they are not in the published folder, so there was nothing more to remove. `UseHarfBuzz()` is required: without it Avalonia throws "No text shaping system configured" (found on the first run). The product's embedded font (Bricolage Grotesque) is not in the spike: its cost is not measured.
  - **d** runtime options: `PublishReadyToRunComposite`, `TieredPGO=false`, `ConcurrentGarbageCollection=false`, `UseSystemResourceKeys=true`. Primary sources (learn.microsoft.com): the ReadyToRun page says composite "compiles a set of assemblies together, allowing the compiler to perform additional optimizations and compile more methods ahead of time", requires self-contained, and makes the application "significantly larger" (here +4.8 MB, one 17 MB `AotWindow.r2r.dll`); the compilation page says quick JIT "decreases startup time" and is on by default (not changed); the other three pages state **no** start-up gain for TieredPGO, concurrent GC or system resource keys. So d is a hypothesis to measure, not a documented gain.
  - **e** software rendering (`Win32PlatformOptions.RenderingMode = [Software]`, no ANGLE/EGL): a diagnostic of what GPU initialisation costs on a VM without a GPU, not necessarily a product choice.
  - **f** b + c + d together.
- **Kit for the VM**: `w7-kit/` (copy of `E:\KaleiVM-W7\`), see its `LEGGIMI.txt`.
- Warm on the dev PC (16 CPUs, not the VM): a ≈ 405 ms, b frame2 ≈ 360 / fullContent ≈ 415, c ≈ 405, d ≈ 328, e ≈ 255, f frame2 ≈ 308 / fullContent ≈ 332. The first run after publish is 2–4x slower (new files). Not W7 values.
