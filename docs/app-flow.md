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

## Transitions
Empty → (drop) → File dropped (or Batch if several files/a folder) → (Convert) → Converting → Done | Error. Module missing appears between "File dropped" and "Target chosen" when the target Format needs an Optional module. Done and Error return to Empty via "Convert another"/dismiss.

## Settings (single page)
Language (System/English/Italiano), Theme (System/Light/Dark), Colour transition on/off, Output folder (next to source / chosen), Name clash behaviour (rename / ask), Limits (Advanced), Installed modules (size, uninstall), Open log folder, Check for updates (opens the Releases page; the only manual network-related action), About and licenses.

## Accessibility
Full keyboard operation; visible focus; screen-reader names for the Drop zone, states and progress; colour is never the only signal (Category name and icon always shown); WCAG AA contrast in both themes; respects Windows "Reduce animations".
