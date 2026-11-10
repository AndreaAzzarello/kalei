# 29 — Accessibility pass

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [12b](12b-in-app-donut-host.md), [14b](14b-batch-window.md), [15](15-name-clash-and-output-folder.md), [24](24-settings-and-persistence.md), [28](28-limits-page.md), [37](37-advanced-panel.md)
- **Rules**: none
- **Stories**: 17, 18, 58, 59, 60 ([user stories](../spec/1.0-windows.md#user-stories))
- **Manual check**: AC 2 (Narrator) and AC 5 (Windows high-contrast theme) are checked on the main reference machine.
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
Everything works without a mouse and with a screen reader, in both themes and with reduced motion.

## Acceptance criteria
1. Every state (Empty, File dropped, Target chosen, Advanced, Converting, Done, Error, Batch, Name clash, and the Donut states D1, D2, D3 and D5) is reachable and operable by keyboard with a visible focus.
2. The Drop zone, the states and the progress have screen-reader names, checked with Narrator.
3. Contrast of the tokens is measured in both themes and meets WCAG AA (4.5:1 text, 3:1 graphics).
4. Windows "Reduce animations" and the "Colour transition" switch are honoured, the stricter winning.
5. In a Windows high-contrast theme every state is checked against a checklist: focus visible, borders visible, text readable, no information carried by colour alone.
6. With protanopia and deuteranopia simulated, the Category states remain distinguishable by name and icon.
