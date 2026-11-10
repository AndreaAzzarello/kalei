# 12 — In-app Donut host: drag over the window, release on a Slice

- **Status**: ready-for-agent
- **Size**: L
- **Blocked by**: [04](04-in-app-donut-spike.md), [10](10-main-window-single-file.md), [11](11-donut-library.md)
- **Rules**: S3
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
The signature gesture: drag files over Kalei, a ring of target Formats opens at the cursor, release on one and the conversion starts with no further window.

## Acceptance criteria
1. Dragging files over the window opens the overlay at the cursor with the Slices from the Donut library; if the files are not yet known it shows "analysing" and fills in.
2. Releasing on a Slice starts the Conversion with no window; a progress ring then the Done notice appear.
3. Releasing outside the Slices gives the normal drop flow; the drop is never lost.
4. Esc or moving away closes the Donut without effect.
5. The same Format list is reachable by keyboard and screen reader in the main window.
6. Windows "Reduce animations" and the "Colour transition" switch make opening and flipping instant.
7. With mixed Categories the Donut shows the intersection; with none the Batch message appears and nothing starts.
8. The source files are unchanged after a drop, which is reported as Copy; the overlay is correct on both monitors of the Windows 11 PC.
