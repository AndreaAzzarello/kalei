# Spike 01: sandbox (W1, W2, W3, W4)

Throwaway code for ticket 01 (issue #1). It lives only on the branch `spike/01-sandbox` and is **never merged into main**; only the measured results go back into the documents.

- `Worker/`: .NET Worker (NetVips + libvips). Receives numeric handle values only; probe modes for network, filesystem, memory, hang and child process.
- `Launcher/`: Win32 launcher (AppContainer without capabilities, `PROC_THREAD_ATTRIBUTE_HANDLE_LIST`, Job Object with memory cap, one-process limit and kill-on-close) and the proof suite. `proofs` runs everything and writes `report.json`.
- `Tests/`: xUnit; each proof is a test on the suite result. Run: `dotnet test spike/01-sandbox/Tests`.
- `build-package.ps1`: builds `artifacts/package`, a folder to copy to the reference VM and run with `run-proofs.ps1` (no SDK needed).

Every negative proof (network, filesystem, child process) has a positive control run without the sandbox; if the control fails the result is INCONCLUSIVE, never PASS.
Timings are warm (file cache not dropped): not the cold timings asked by ticket-0.
