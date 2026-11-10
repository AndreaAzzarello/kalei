# Kalei — Format detection (draft v0)

Rules behind S3 ([architecture.md](architecture.md#security-requirements-normative-the-only-place-they-are-written)): the type comes from content; the extension is only a hint.

## Two stages
1. **Header stage (Core)**: reads at most the first 64 KB and matches the signatures below. Result: a *candidate* Format, or "text-like" (no binary signature), or "unknown".
2. **Probe stage (Worker, `Probe` message)**: confirms the candidate by parsing in the sandbox, and decides the text-like cases. The Core never parses structure itself.

The extension may only break a tie **among text-like candidates** (for example TXT or MD); it never overrides a binary signature. If the extension and the content disagree, the user is told ("This looks like PNG, not JPG") and the content wins.

## Signatures (header stage)
| Format | Signature or rule | Bytes read | If it fails |
|---|---|---|---|
| PNG | `89 50 4E 47 0D 0A 1A 0A` | 8 | unknown |
| JPG | `FF D8 FF` | 3 | unknown |
| GIF | `GIF87a` or `GIF89a` | 6 | unknown |
| WebP | `RIFF` at 0, `WEBP` at 8 | 12 | unknown |
| BMP | `BM` at 0 and a valid header size (12, 40, 52, 56, 108 or 124) at 14 | 18 | unknown |
| TIFF | `49 49 2A 00` or `4D 4D 00 2A` | 4 | unknown |
| ICO | `00 00 01 00` and image count 1–255 (`00 00 02 00` is CUR: not supported) | 6 | unknown |
| AVIF / HEIC | `ftyp` at offset 4; major brand `avif` or `avis` → AVIF; `heic`, `heix`, `hevc`, `hevx` → HEIC; `mif1` or `msf1` → look at the compatible brands: `avif` present → AVIF, `heic` present → HEIC, neither → unknown | up to 64 KB | unknown |
| XLSX | ZIP signature `50 4B 03 04`; the Probe reads the ZIP directory and requires `[Content_Types].xml` and `xl/workbook.xml` | 4 (Core), directory in the Worker | other ZIP content → **Other**, no conversion offered (archives are a later Module) |
| JSON | text-like; after an optional BOM and whitespace the first character is `{` or `[`; the Probe must parse it | up to 64 KB | text-like fallthrough |
| XML | text-like; after BOM and whitespace starts with `<?xml` or `<`; the Probe parses it with DTD and external entities disabled | up to 64 KB | text-like fallthrough |
| HTML | text-like; within the first 1 KB (case-insensitive) `<!doctype html` or `<html` | 1 KB | text-like fallthrough |
| YAML | text-like; no signature. Accepted only when it parses as YAML **and** is not valid JSON, and either starts with `---` or the extension is `.yaml`/`.yml` | Probe | TXT |
| CSV / TSV | text-like; no signature. The Probe picks a delimiter (`,` `;` tab) that gives the same non-zero column count on at least 90% of the first 20 non-empty lines; tab-delimited → TSV | Probe, up to 1 MB | TXT |
| MD | text-like; indistinguishable from TXT by content, so the extension (`.md`, `.markdown`) breaks the tie | Probe | TXT |
| TXT | valid text with no other rule matching | Probe | – |

## Text encoding
A BOM decides first (UTF-8, UTF-16 LE/BE). Without a BOM: strict UTF-8 if the whole probed part is valid UTF-8, otherwise Windows-1252. The result is shown in Advanced and can be overridden. CSV delimiter, encoding and decimal separator (`1.234,56`) are detected in the Probe and overridable.

## Unknown or ambiguous
"Cannot be determined" → Category **other**, with the message "Kalei doesn't recognise this file" and no conversion offered. An ambiguous text-like file is shown with the candidate chosen and a way to pick another in the target list's "Treat as…" option (decided in the spec).

## Acceptance
`tests/samples/` holds, for every Format, at least one valid file, one truncated, one with the wrong extension, and one generated large file (not committed); the detection test asserts the Format, and for failures the exact error code ([ipc-spec.md](ipc-spec.md#error-codes)).
