# 13 — Probe and Preview in the Worker: safe previews and thumbnails

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [07a](07a-ipc-protocol-and-worker-host.md), [10a](10a-main-window-file-card.md)
- **Rules**: S2, S3
- **Stories**: 48, 49 ([user stories](../spec/1.0-windows.md#user-stories))
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
The user sees a preview of the file on the file card, produced by a sandboxed Worker.

## Acceptance criteria
1. Probe and Preview are answered as specified in IPC v1; the preview is returned as bounded PNG data (at most 4 MB).
2. Images show a thumbnail on the file card (the first lines of text and data files are added by tickets [20a](20a-data-csv-tsv.md), [21](21-data-tree.md) and [23](23-text-module.md)).
3. The app decodes only that bounded PNG, checking its size and dimensions before decoding; the dependency test of ticket [05](05-solution-skeleton.md) shows that no other image or document decoder or parser is referenced.
4. Previews run under the same sandbox and Limits as a Conversion; a failed preview shows a neutral icon, not an error.
5. Preview requests are debounced by 150 ms and at most 2 Preview Workers run at once; the others wait.
