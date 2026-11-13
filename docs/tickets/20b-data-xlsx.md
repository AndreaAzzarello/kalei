# 20b — Data: XLSX with CSV and TSV

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [20a](20a-data-csv-tsv.md)
- **Rules**: S2, S9
- **Stories**: 41, 44 ([user stories](../spec/1.0-windows.md#user-stories))
- **Manual check**: AC 8 (the written XLSX opens in Excel and in LibreOffice Calc) is run by the owner.
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
A user converts spreadsheets between XLSX and CSV or TSV safely.

## Acceptance criteria
1. CSV and TSV convert to and from XLSX; the first sheet is used; formulas are exported as their cached value.
2. Nothing is written as a formula: text is an explicit string cell.
3. Before the XLSX library reads a file, a pre-scan of the ZIP directory checks the expansion ratio, the total size and nested archives against the Limits table; a violation gives E_BOMB; malicious samples cover each limit.
4. The preselected targets of CSV (XLSX) and XLSX (CSV) match the spec table; the card shows the first rows.
5. Samples: valid, truncated, wrong extension, malicious XLSX; tests assert the exact cells the writer produces.
6. Reading uses Sylvan.Data.Excel at the exact version 0.5.8 (no version range; MIT, checked in ticket [03a](03a-build-spike.md)). It is a 0.x library, so any change of the pin is reviewed first: release notes, trimmed publish with warnings as errors, the smoke test on the trimmed binary and the malicious samples. Its own writer is not used (it fails trim analysis, W9).
7. Writing is our own minimal code in the Worker: text cells are inline strings (`t="inlineStr"`), no shared strings; a cell is written as a number with the explicit numeric type only if its text matches `-?(0|[1-9][0-9]*)(\.[0-9]+)?` (so `00123` and `+1` stay text); invariant formatting; never a formula; no style beyond the minimum Excel needs to open the file (a minimal styles part only if the test in criterion 8 shows it is required); the first sheet only.
8. Manual check: a written XLSX (text, numbers, `=1+1` as text, leading zeros, accents, a long text, an empty cell) opens without a repair prompt in Microsoft Excel and in LibreOffice Calc, texts stay texts, numbers are numbers; the result is written in HISTORY with the versions of both programs.
9. Globalization constraint ([TRD](../TRD.md#culture-and-globalization-decided-2026-11-13)): number and date text is read and written with explicit `NumberFormatInfo` and `DateTimeFormatInfo` defined in code, never the OS culture, so the Worker may run in invariant mode; covered by the Italian-number test of ticket [20a](20a-data-csv-tsv.md) on the published Worker.
