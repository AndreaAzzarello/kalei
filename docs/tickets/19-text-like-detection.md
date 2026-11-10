# 19 — Detection of text-like Formats in the Probe

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [06](06-header-detection-images.md), [07a](07a-ipc-protocol-and-worker-host.md)
- **Rules**: S3, S9
- **Stories**: 3, 4 ([user stories](../spec/1.0-windows.md#user-stories))
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
Kalei tells CSV, TSV, JSON, XML, YAML, HTML, Markdown and plain text apart, with the right encoding, so that the right conversions are offered.

## Acceptance criteria
1. The Probe decides among TXT, MD, CSV, TSV, JSON, XML, YAML and HTML by the rules of the format detection document.
2. Encoding: a BOM first (UTF-8, UTF-16 LE or BE), then strict UTF-8, else Windows-1252.
3. The CSV delimiter is the one giving a constant non-zero column count on at least 90% of the first 20 non-empty lines.
4. The extension breaks ties only among text-like candidates (for example MD or TXT).
5. An ambiguous file is shown with the chosen candidate and a notice; there is no way to pick another candidate in 1.0.
6. Samples and tests cover Italian CSV with ; and decimal commas, Windows-1252 text, UTF-16 text and wrong-extension files.
