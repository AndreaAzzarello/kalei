# 13 — Probe and Preview in the Worker: safe previews and thumbnails

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [07](07-launcher-and-worker-ipc.md), [10](10-main-window-single-file.md)
- **Rules**: S3
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
The user sees a preview of the file on the file card, produced by a sandboxed Worker, so that no untrusted bytes are ever decoded by the app itself.

## Acceptance criteria
1. The Probe and Preview messages work as specified, with the preview returned as bounded PNG data (at most 4 MB).
2. Images show a thumbnail on the file card; text and data Formats show their first lines once their Modules exist.
3. A test proves the app project has no image or document decoder and never parses file content.
4. Previews run under the same sandbox and Limits as a Conversion; a failed preview shows a neutral icon, not an error.
5. Preview requests are debounced so that quickly changing files does not queue Workers without bound.
