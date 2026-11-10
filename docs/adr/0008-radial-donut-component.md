---
status: accepted
---
# One radial Donut component, two hosts: in-app (1.0) and global (1.2)

The Donut is a ring of Slices opened at the cursor while files are dragged. Dropping on a Slice starts the Conversion with no window. It is built once as a component whose geometry, slice layout and hit-testing are pure code with no UI dependency, plus a thin Avalonia drawing layer.

- **In-app Donut (1.0)**: files dragged over Kalei's own window (ordinary OLE drag-and-drop) open the Donut as an overlay inside the window. No global hook, no tray, no code signing required.
- **Global Donut (1.2)**: opt-in; the same component is shown in a transparent, topmost, non-activating window, opened by the Sentinel ([ADR 0009](0009-mouse-only-hook-and-sentinel.md)). It ships only with a signed build.

Each Donut has two Faces: front (up to 8 common Formats of the Category), back (less common ones); one fixed profile per Category; presets are only quality and size. With mixed Categories the Donut offers only Formats valid for every file; if there is none, the normal window flow is used. A Slice whose Module is missing is disabled with the reason, and releasing on it opens the consent card (never a silent download).

Accessibility: the Donut is a shortcut, not the only way. The main window and (from 1.1) the Explorer menu offer the same Format list, fully keyboard and screen-reader operable. Faces flip with the mouse wheel; Tab only in the in-app host, where the window has focus. The Donut honours "Reduce animations".

## Considered options

- Global Donut already in 1.0: needs a signed build, a resident process and an antivirus-sensitive hook before the product is proven.
- No Donut until 1.2: loses the product's distinctive gesture for the whole first release.
