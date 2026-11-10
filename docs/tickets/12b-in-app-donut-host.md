# 12b — In-app Donut host: drag over the window, release on a Slice

- **Status**: ready-for-agent
- **Size**: L
- **Blocked by**: [04](04-in-app-donut-spike.md), [07a](07a-ipc-protocol-and-worker-host.md), [10b](10b-main-window-convert.md), [12a](12a-donut-control.md)
- **Rules**: S3, S4
- **Stories**: 12, 14, 15, 16, 17, 19 ([user stories](../spec/1.0-windows.md#user-stories))
- **Manual check**: AC 6 is checked on the Windows 11 PC with two monitors.
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
The signature gesture: drag files over Kalei, a ring of target Formats opens at the cursor, release on one and the conversion starts with no further window.

## Acceptance criteria
1. Dragging files over the window opens the overlay at the cursor with the Slices from the Donut library; if the files are not yet known it shows "analysing" (using the Probe) and fills in.
2. Releasing on a Slice starts the Conversion with no window; a progress ring and then the Done notice appear.
3. Releasing outside the Slices gives the normal drop flow; the drop is never lost.
4. Esc or moving away closes the Donut without effect.
5. The same Format list stays reachable by keyboard and screen reader in the main window (ticket [10a](10a-main-window-file-card.md)).
6. The source files are unchanged after a drop, which is reported as Copy; the overlay is correct on both monitors of the Windows 11 PC at 100%, 150% and 200% scaling.
