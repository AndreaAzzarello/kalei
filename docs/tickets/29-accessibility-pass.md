# 29 — Accessibility pass

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [12](12-in-app-donut-host.md), [24](24-settings-and-persistence.md)
- **Rules**: none
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
Everything works without a mouse and with a screen reader, in both themes and with reduced motion.

## Acceptance criteria
1. Every state, including the Donut equivalent, is reachable and operable by keyboard with a visible focus.
2. The Drop zone, states and progress have screen-reader names, checked with Narrator on the reference machine.
3. Contrast of the tokens is measured in both themes and meets WCAG AA (4.5:1 text, 3:1 graphics).
4. Windows "Reduce animations" and the "Colour transition" switch are honoured, the stricter winning.
5. The app remains usable in a Windows high-contrast theme.
