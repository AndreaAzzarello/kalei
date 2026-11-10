# 20b — Data: XLSX with CSV and TSV

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [20a](20a-data-csv-tsv.md)
- **Rules**: S2, S9
- **Stories**: 41, 44 ([user stories](../spec/1.0-windows.md#user-stories))
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
A user converts spreadsheets between XLSX and CSV or TSV safely.

## Acceptance criteria
1. CSV and TSV convert to and from XLSX; the first sheet is used; formulas are exported as their cached value.
2. Nothing is written as a formula: text is an explicit string cell.
3. Before the XLSX library reads a file, a pre-scan of the ZIP directory checks the expansion ratio, the total size and nested archives against the Limits table; a violation gives E_BOMB; malicious samples cover each limit.
4. The preselected targets of CSV (XLSX) and XLSX (CSV) match the spec table; the card shows the first rows.
5. Samples: valid, truncated, wrong extension, malicious XLSX.
