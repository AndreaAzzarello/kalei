# Spike 04: in-app Donut (W10)

THROWAWAY code for ticket 04. Branch `spike/04-donut`, never merged. Results go to `docs/` through a documentation-only PR.

An Avalonia 12.1.6 window opens a radial overlay (ring with up to 8 slices) when files are dragged over it, and logs:
- time from the first drag-enter handler call to the overlay in layout (`frame1Ms`) and to the next frame tick (`frame2Ms`, the figure to compare with 100 ms);
- what is known at drag-enter: file count, extension-based Format (`formats`), header-based Format of the first file (`headerFormat`, 16 bytes read after the overlay is up);
- screen index and scaling, whether the window straddles a screen edge, whether the ring stays inside the window;
- at drop: the drop effect reported (`Copy`), and whether the first source file is unchanged (SHA-256 before and after 500 ms, size, last-write time).

Logs hold only extensions, Formats, sizes and timings. No file names, paths or user names. Default folder: `%LOCALAPPDATA%\KaleiSpike04\`.

Limits of the measure: time zero is the first line of the drag-enter handler, so the OS-to-handler delay is not included; frame ticks come from `TopLevel.RequestAnimationFrame` (callback 1 = the tick whose frame includes the overlay, callback 2 = the following tick). If no tick arrives in 1 s the row is flagged `NO_FRAME_IN_1S`.

## Run (PowerShell, from this folder)

```powershell
dotnet test .\DonutSpike.Tests          # 32 tests on the pure logic
.\make-samples.ps1                      # creates sample files in %TEMP%\KaleiSpike04Samples
.\run-cold.ps1 -Tag win11-100-mon0 -Runs 10 -Screen 0
.\summarize.ps1 -Tag win11-100-mon0
```

`run-cold.ps1` starts a new process for each run (a cold first drag), waits for you to drag the sample files over the window and drop them once, and the window closes by itself. Options: `-Screen n` opens the window centred on screen n (index as shown in the window text after the first drag), `-Straddle` with `-Screen n` puts the window centre on the right edge of screen n.

## Log fix (second version)
The first version wrote each latency row from a 1 s timer after the drag-enter. With `--exit-after-drop` the window closes about 0.5 s after the drop, so a drop within roughly 0.5 s of the drag-enter exited before the row was written (one row lost in most series). It also read the sequence number at write time and counted re-entries in the same process (warm) next to the cold first drag. Now the row is written at the second frame (or at the drop, flag `NO_FRAME_BEFORE_DROP`, or after 1 s, flag `NO_FRAME_IN_1S`), the sequence is captured at enter, and the column `enter` is 1 for the first drag-enter of a process. `summarize.ps1` uses only `enter=1`; `run-cold.ps1` repeats a run that left no valid cold row.

## Run on a machine without the SDK (the reference VM)
```powershell
dotnet publish .\DonutSpike -c Release -r win-x64 --self-contained -o ..\..\artifacts\donut
# copy artifacts\donut, run-cold.ps1, summarize.ps1 and the samples to the VM, then there:
.\run-cold.ps1 -Exe .\donut\DonutSpike.exe -Tag vm-win10 -Runs 10
```
