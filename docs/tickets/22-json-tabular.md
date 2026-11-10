# 22 — Data: JSON to and from tables

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [20](20-data-tabular.md), [21](21-data-tree.md)
- **Rules**: S9
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
A user turns a JSON array into a spreadsheet or CSV and back, with predictable flattening.

## Acceptance criteria
1. A JSON array of objects converts to CSV, TSV and XLSX; nested values use dot notation, arrays `a.0`, `a.1` or a configurable separator.
2. An array of plain values becomes a single column `value`; ragged objects give the union of keys with empty cells; JSON that is not an array gives a clear error.
3. A table converts to a JSON array of objects, recognising numbers including Italian numbers.
4. Round-trip tests keep the data for the supported shapes.
