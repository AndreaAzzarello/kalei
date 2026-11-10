# 10a — Main window: Drop zone, file card, target list and Category accent

- **Status**: ready-for-agent
- **Size**: L
- **Blocked by**: [09a](09a-images-module-core.md)
- **Rules**: S3, S6, S10
- **Stories**: 1, 2, 3, 4, 5, 6, 7, 54, 60 ([user stories](../spec/1.0-windows.md#user-stories))
- **Manual check**: AC 4 (colours, icon, Reduce animations) is checked by eye on the main reference machine.
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
A user drops one file and sees what it is, which target Format is proposed and which others are possible, in a window that takes the colour of the file.

## Acceptance criteria
1. The states Empty, File dropped and Target chosen of the app flow work for a single file, including Choose files reachable by keyboard.
2. The file card shows the real Format, the size and the mismatch notice; animated images show the note "first frame only".
3. The target list offers the valid Formats (the source Format excluded, except TXT keeping TXT) with the preselected default from the spec; the preselection table is tested for the Formats that exist now (images), and each later Module ticket adds its own.
4. The accent and the Kalei icon in the window take the Category colour; the Category name and icon are always shown next to the colour; light and dark follow Windows; with Windows "Reduce animations" on, colour changes take 0 ms (no animation frames).
5. Every string is a keyed resource with English and Italian text, and the mechanism works in a trimmed published build.
6. The window never decodes untrusted content (the dependency test of ticket [05](05-solution-skeleton.md)) and contains no network or update-check code (S6).
