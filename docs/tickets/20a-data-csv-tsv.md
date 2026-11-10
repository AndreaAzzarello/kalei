# 20a — Data: CSV and TSV

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [07b](07b-windows-sandbox-launcher.md), [19](19-text-like-detection.md), [09a](09a-images-module-core.md)
- **Rules**: S2, S9
- **Stories**: 38, 41, 44 ([user stories](../spec/1.0-windows.md#user-stories))
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
A user converts between CSV and TSV, including Italian files, safely, and sees the first lines of the file on the card.

## Acceptance criteria
1. CSV and TSV convert to each other.
2. Separator and encoding are detected and can be overridden through the options (the panel is ticket [37](37-advanced-panel.md)); UTF-16 with a BOM is read; the output is UTF-8 or the chosen encoding; Windows-1252 output replaces characters it cannot hold with ? and gives the notice LOSSY_ENCODING.
3. Italian numbers (1.234,56) are understood.
4. Text cells starting with =, +, - or @ are neutralised by default (switch in the options); cells that hold a number are written as they are (S9).
5. A file one byte over the data and text size limit gives E_SIZE.
6. The file card shows the first lines of a CSV or TSV file (through Preview), and the preselected target of TSV is CSV as in the spec table.
7. Samples: valid, truncated, wrong extension, Italian with ;, Windows-1252, UTF-16.
