# Spike 02: image engine content (W5, W6)

Throwaway code for ticket 02 (issue #2). It lives only on the branch `spike/02-image-engine` and is **never merged into main** (see docs/ticket-0.md: spike code is not product code). Only the measured results go back into the documents.

Run: `dotnet test spike/02-image-engine/ImageEngineSpike.Tests --logger "console;verbosity=detailed"`

- `EngineContentTests.cs`: W6, what the prebuilt libvips (NetVips 3.2.0, NetVips.Native.win-x64 8.18.7) contains.
- `HeicTests.cs`: W5 outside the sandbox, HEIC through WIC (WPF `BitmapDecoder`, spike only).
- `results/test-output.sanitized.txt`: output of the run on the owner's Windows 11 PC (local paths removed).

A missing feature is recorded as a result: those tests assert the absence.
Pixel decode is forced (`Avg()`), because `Image.NewFromBuffer` is lazy and reads only the header.
