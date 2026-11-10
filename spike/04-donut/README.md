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
