# 37 — Advanced panel: options for the pair, reset, CSV overrides

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [09b](09b-images-module-behaviours.md), [10b](10b-main-window-convert.md), [20a](20a-data-csv-tsv.md), [22](22-json-tabular.md)
- **Rules**: S9
- **Stories**: 30, 31, 34, 38 ([user stories](../spec/1.0-windows.md#user-stories))
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
The user can change quality, size, metadata and CSV choices in an Advanced panel that shows only what makes sense for the chosen pair.

## Acceptance criteria
1. The Advanced state shows only the options valid for the selected pair: images: quality, maximum size, strip all metadata; CSV and TSV: separator, encoding (the detected value shown, overridable) and the neutralisation switch; JSON to a table: the array separator; text: none.
2. Reset to defaults restores the defaults of the spec.
3. Values are kept only while the app runs; the defaults apply at the next start.
4. Options are validated against the manifest; an unknown option gives E_UNSUPPORTED_OPTION.
5. The panel is operable by keyboard and screen reader.
