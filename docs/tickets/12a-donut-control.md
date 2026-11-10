# 12a — Donut control: drawing, hover and Reduce animations

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [10a](10a-main-window-file-card.md), [11](11-donut-library.md)
- **Rules**: S3
- **Stories**: 13, 18 ([user stories](../spec/1.0-windows.md#user-stories))
- **Manual check**: AC 1 and 2 are checked by eye on the main reference machine.
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
The Avalonia control that draws the Donut from the library, ready to be hosted by the window.

## Acceptance criteria
1. An Avalonia control draws the Slices and the hub with the Bloom in the Category accent; the Slice under the cursor takes the accent, the others stay neutral.
2. Opening is a 160 ms scale and fade; with Windows "Reduce animations" on, or Kalei's "Colour transition" off (the switch arrives in ticket [24](24-settings-and-persistence.md)), it is instant.
3. Text contrast is at least 4.5:1 and graphics contrast at least 3:1 in both themes (measured).
4. Every Slice has a screen-reader name.
