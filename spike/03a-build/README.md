# Spike 03a: build (W7, W8 first estimate, W9)

Throwaway code for ticket 03a (issue #3). It lives only on the branch `spike/03a-build` and is **never merged into main**; only the measured results go back into the documents.

- `AotWindow/`: minimal Avalonia 12.1.6 + Skia window (one AXAML view, compiled bindings on). It measures its own start-up (process creation to the second animation frame after the window opened) and, with `--out file`, writes it and exits.
  - Native AOT: `dotnet publish AotWindow -c Release -r win-x64 -p:PublishAot=true -o artifacts/aot` (needs the MSVC linker: "Desktop development with C++").
  - Fallback: `dotnet publish AotWindow -c Release -r win-x64 --self-contained -p:PublishReadyToRun=true -p:PublishTrimmed=true -o artifacts/r2r`.
  - `measure.ps1` runs 10 start-ups and measures folder and ZIP size without `.pdb`.
- `TrimmedWorker/`: self-contained trimmed Worker with every 1.0 library and a smoke test that runs on the trimmed binary (trimming can break reflection without any warning). Publish: `dotnet publish TrimmedWorker -c Release -o artifacts/worker-b` (warnings are errors).
- `results/`: sanitised outputs. Run A = libraries as listed in the TRD (YamlDotNet typed API, MiniExcel); run B = YamlStream + Sylvan.Data.Excel reader + own XLSX writer, with warnings as errors.

Timings are warm (file cache not dropped, no reboot): not the cold timings asked by ticket-0.
