# 27 — Drops from a browser and the origin mark

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [08](08-broker-and-output-writing.md), [10b](10b-main-window-convert.md)
- **Rules**: S7, S12
- **Stories**: 50, 51, 52 ([user stories](../spec/1.0-windows.md#user-stories))
- **Manual check**: AC 1 and 2 are tried with a real browser on the main reference machine.
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
A user can drag a file from the browser and convert it; Kalei never downloads a web address, and the result keeps the "from the internet" mark without the address.

## Acceptance criteria
1. A browser drop that arrives as a stream is written to Kalei's temporary folder under the Limits and removed after the Conversion, on error, on cancel and at the next start.
2. A dropped web address with no bytes shows "Save the file first" and nothing is downloaded.
3. The Output file keeps the origin mark with the zone only: no referrer or host address is copied.
4. On a destination that cannot hold the mark, the Conversion still runs, the notice NO_ORIGIN_MARK is shown and the user is told.
5. Ordinary file drops from the file manager behave as before.
