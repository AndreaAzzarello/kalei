# Kalei — App flow

One window. The **Drop zone** is the centre of every state. Terms: see [GLOSSARY.md](../GLOSSARY.md).

## States

| # | State | User sees | User can |
|---|---|---|---|
| 1 | **Empty** | Neutral Kalei icon, Drop zone with "Drop files or folders here", **Choose files** button (keyboard reachable), gear (Settings). | Drop, Choose files (Enter/Space), open Settings. |
| 2 | **File dropped** | Icon and Accent switch to the file's Category; file name, detected real Format, size, preview; target Format list with the most common preselected; **Estimate** "≈ 340 KB". If the extension disagrees with the content, a notice "This looks like PNG, not JPG". | Pick target Format, open Advanced, change Output folder, Convert, remove file, add more files. |
| 3 | **Module missing** | Card "Video needs the Video module (80 MB). Install?" with source and size; nothing downloaded yet. | Install (consent), Install from file (.kmod), Cancel. |
| 3b | **Installing module** | Progress, then "Hash verified". On hash mismatch: error, nothing installed. | Cancel. |
| 4 | **Target chosen** | Same as 2, with Convert enabled and the Estimate updated. | Convert, change anything. |
| 5 | **Advanced options** | Panel with only the Options valid for this Format pair (quality, resolution, metadata, CSV separator/encoding, sheets…) and a "Reset to defaults". | Edit, reset, close. |
| 6 | **Converting** | Progress bar in the Accent colour; Cancel. | Cancel (Worker is killed, temp output removed). |
| 7 | **Done** | "Converted", output name and real size, **Show in folder**, **Convert another**. | Show in folder, open file, convert another. |
| 8 | **Error** | Plain-language message and short code (e.g. "The file is damaged", "File too large (limit 2 GB)", "Out of memory limit"); no partial output left behind. | Retry, change options, dismiss, Open log folder. |
| 9 | **Batch** | List of files with per-file status (queued, converting, done, error, skipped), one target Format per Category, overall progress, count of done/failed. | Change target per Category, Convert all, Cancel all, retry failed, Show in folder. |
| 10 | **Name clash** (only when "ask every time") | Dialog with two choices, **Keep both** (saves as `name (1).ext`) or **Skip**, plus "Apply to all". Replace is never offered. | Choose, tick "Apply to all". |

## In-app Donut states ([ADR 0008](adr/0008-radial-donut-component.md))

| # | State | User sees | User can |
|---|---|---|---|
| D1 | **Dragging over the window** | The Donut opens at the cursor: front Face with up to 8 common target Formats for the files' Category (most common marked); disabled Slices show the reason (e.g. "needs the Video module"); hub shows the Bloom in the Category accent and "Mouse wheel: more formats". | Move over a Slice (it takes the accent); turn the mouse wheel or press Tab to flip to the back Face; move away or press Esc to close. |
| D2 | **Release on a Slice** | Donut closes; a small progress ring appears where it was, then the normal toast/Done state. No window needed. | Cancel from the ring or the toast. |
| D3 | **Release outside the Slices** | Donut closes; normal "File dropped" state (2). | Continue as usual. |
| D4 | **Release on a disabled Slice** | If the Module is missing: the consent card (state 3). Otherwise a short notice with the reason. Nothing is downloaded before consent. | Install, install from file, cancel. |
| D5 | **Mixed Categories** | Donut offers only Formats valid for all files; with none, the Donut does not open and the Batch state (9) is used. | As in 9. |

Keyboard and screen-reader users reach the same Format list in the main window (state 2) and, from 1.1, in the Explorer menu. The Donut honours Windows "Reduce animations" and Kalei's "Colour transition" switch (the hub dot changes colour instantly when off).

## Transitions
Empty → (drop) → File dropped (or Batch if several files/a folder) → (Convert) → Converting → Done | Error. Module missing appears between "File dropped" and "Target chosen" when the target Format needs an Optional module. Done and Error return to Empty via "Convert another"/dismiss.

## Settings (single page)
Language (System/English/Italiano), Theme (System/Light/Dark), Colour transition on/off, Output folder (next to source / chosen), Name clash behaviour (rename / ask), Limits (Advanced), Installed modules (size, uninstall), Open log folder, Check for updates (opens the Releases page; the only manual network-related action), About and licenses. From 1.2: Global Donut (off by default), modifier key, start at sign-in (off by default); enabling the Global Donut explains the resident Sentinel and the mouse-only hook before turning it on.

## Accessibility
Full keyboard operation; visible focus; screen-reader names for the Drop zone, states and progress; colour is never the only signal (Category name and icon always shown); WCAG AA contrast in both themes; respects Windows "Reduce animations".
