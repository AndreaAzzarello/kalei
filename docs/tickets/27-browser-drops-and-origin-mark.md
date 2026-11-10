# 27 — Drops from a browser and the origin mark

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [08](08-broker-and-output-writing.md), [10](10-main-window-single-file.md)
- **Rules**: S7, S12
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
A user can drag a file from the browser and convert it; Kalei never downloads a link, and the result keeps the "from the internet" mark without the address.

## Acceptance criteria
1. A browser drop that arrives as a stream is spooled to Kalei's own temporary folder under the Limits and removed after the Conversion, on error, on cancel and at the next start.
2. A dropped link with no bytes shows "Save the file first" and nothing is downloaded.
3. The Output keeps the origin mark with the zone only: no referrer or host address is copied.
4. On a destination that cannot hold the mark, the Conversion still runs and the user is told.
5. Ordinary file drops from the file manager behave as before.
