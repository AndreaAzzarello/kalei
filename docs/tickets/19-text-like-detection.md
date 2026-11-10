# 19 — Detection of text-like Formats in the Probe

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [06](06-header-detection-images.md), [07](07-launcher-and-worker-ipc.md)
- **Rules**: S3, S9
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
Kalei tells CSV, TSV, JSON, XML, YAML, HTML, Markdown and plain text apart, with the right encoding, so that the right conversions are offered.

## Acceptance criteria
1. The Probe decides among TXT, MD, CSV, TSV, JSON, XML, YAML and HTML by the rules of the format detection document.
2. Encoding: BOM first, then strict UTF-8, else Windows-1252.
3. The CSV delimiter is the one giving a constant non-zero column count on at least 90% of the first 20 non-empty lines.
4. The extension breaks ties only among text-like candidates (for example MD or TXT).
5. An ambiguous file is shown with the chosen candidate and a notice.
6. Samples and tests cover Italian CSV with `;` and decimal commas, Windows-1252 text and wrong-extension files.
