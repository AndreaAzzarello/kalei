# 20 — Data: CSV, TSV and XLSX

- **Status**: ready-for-agent
- **Size**: L
- **Blocked by**: [07](07-launcher-and-worker-ipc.md), [19](19-text-like-detection.md)
- **Rules**: S2, S9
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
A user converts spreadsheets and delimited files among CSV, TSV and XLSX, including Italian files, safely.

## Acceptance criteria
1. All pairs among CSV, TSV and XLSX work.
2. Separator and encoding are detected and can be overridden; Italian numbers (`1.234,56`) are understood.
3. XLSX uses the first sheet by default; "all sheets" gives one CSV per sheet; formulas are exported as cached values.
4. Nothing is written as a formula: text is an explicit string cell; CSV and TSV output neutralises cells starting with `=`, `+`, `-` or `@` by default, with a switch (S9).
5. A malicious or bomb-like XLSX ends in E_BOMB within the ratio (100:1), total (1 GB) and no-nested-archive limits; the 200 MB file limit is enforced.
6. Samples include valid, truncated, wrong-extension and malicious XLSX files.
