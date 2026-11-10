# 10a — Main window: Drop zone, file card, target list and Category accent

- **Status**: ready-for-agent
- **Size**: L
- **Blocked by**: [09a](09a-images-module-core.md)
- **Rules**: S3, S6, S10
- **Stories**: 1, 2, 3, 4, 5, 6, 7, 54, 60 ([user stories](../spec/1.0-windows.md#user-stories))
- **Manual check**: AC 4 (colours, icon, Reduce animations) is checked by eye on the main reference machine; AC 6 (size) is also looked at by eye at 1366×768 and 1920×1080 at 200%.
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
A user drops one file and sees what it is, which target Format is proposed and which others are possible, in a window that takes the colour of the file.

## Acceptance criteria
1. The states Empty, File dropped and Target chosen of the app flow work for a single file, including Choose files reachable by keyboard.
2. The file card shows the real Format, the size and the mismatch notice; animated images show the note "first frame only".
3. The target list offers the valid Formats (the source Format excluded, except TXT keeping TXT) with the preselected default from the spec; the preselection table is tested for the Formats that exist now (images), and each later Module ticket adds its own.
4. The accent and the Kalei icon in the window take the Category colour; the Category name and icon are always shown next to the colour; light and dark follow Windows; with Windows "Reduce animations" on, colour changes take 0 ms (no animation frames).
5. Every string is a keyed resource with English and Italian text, and the mechanism works in a trimmed published build.
6. The initial size and the minimum size of the window fit a 1366×768 screen at 100% and a 1920×1080 screen at 200% (usable area about 960×540 DIP, less the taskbar), with no clipped content in any state; tested by layout at both sizes. Reason: the spike window (900×620 DIP) did not fit the second case ([ticket-0, W10](../ticket-0.md#w10--in-app-donut-spike-ticket-04-run-2026-11-10)).
7. The window never decodes untrusted content (the dependency test of ticket [05](05-solution-skeleton.md)) and contains no network or update-check code (S6).
