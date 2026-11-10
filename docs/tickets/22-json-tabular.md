# 22 — Data: JSON to and from tables

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [20b](20b-data-xlsx.md), [21](21-data-tree.md)
- **Rules**: S9
- **Stories**: 39 ([user stories](../spec/1.0-windows.md#user-stories))
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
A user turns a JSON array into a spreadsheet or CSV and back, with predictable flattening.

## Acceptance criteria
1. A JSON array of objects converts to CSV, TSV and XLSX; nested values use dot notation, arrays a.0, a.1 or a configurable separator.
2. An array of plain values becomes a single column value; ragged objects give the union of keys with empty cells; JSON that is not an array gives E_FORMAT with a clear message.
3. A table converts to a JSON array of objects, recognising numbers including Italian numbers.
4. Round-trip tests keep the data for the supported shapes.
