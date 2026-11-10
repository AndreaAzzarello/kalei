---
status: accepted
---
# One radial Donut component, two hosts: in-app (1.0) and global (1.2)

The Donut is a ring of Slices opened at the cursor while files are dragged. Dropping on a Slice starts the Conversion with no window. It is built once as a component whose geometry, slice layout and hit-testing are pure code with no UI dependency, plus a thin Avalonia drawing layer.

- **In-app Donut (1.0)**: files dragged over Kalei's own window (ordinary OLE drag-and-drop) open the Donut as an overlay inside the window. No global hook, no tray, no code signing required.
- **Global Donut (1.2)**: opt-in; the same component is shown in a transparent, topmost, non-activating window, opened by the Sentinel ([ADR 0009](0009-mouse-only-hook-and-sentinel.md)). It ships only with a signed build.

Each Donut has a front Face (up to 8 common target Formats, the source Format excluded except TXT → TXT) and, only when more targets exist, a back Face with the rest; the hint to flip is hidden when there is no back Face. One fixed profile per Category; presets are only quality and size. With mixed files the Donut offers only the Formats that every file can be converted to or already is; if there is none, the Donut does not open and the Batch state asks to split the files. From 1.1 a Slice whose Module is missing is shown disabled with the reason, and releasing on it opens the consent card (never a silent download); in 1.0 there are no Optional modules and so no such Slice.

Accessibility: the Donut is a shortcut, not the only way. The main window and (from 1.1) the file-manager menu offer the same Format list, fully keyboard and screen-reader operable. In 1.0 there is only the front Face (at most 8 targets), so nothing flips; when a back Face exists, Faces flip with the mouse wheel (Tab only in the in-app host, where the window has focus; wheel events during a drag are checked in ticket 34). The Donut honours "Reduce animations".

## Considered options

- Global Donut already in 1.0: needs a signed build, a resident process and an antivirus-sensitive hook before the product is proven.
- No Donut until 1.2: loses the product's distinctive gesture for the whole first release.
