# 06 — Format detection from the header for images

- **Status**: ready-for-agent
- **Size**: S
- **Blocked by**: [05](05-solution-skeleton.md)
- **Rules**: S3
- **Stories**: 3, 4 ([user stories](../spec/1.0-windows.md#user-stories))
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
The Core recognises the real image Format of a file from its first bytes, ignores a lying extension, and says "not recognised" for anything else.

## Acceptance criteria
1. Signatures of PNG, JPG, GIF, WebP, BMP, TIFF, ICO and the AVIF and HEIC brands are detected as in the format detection document.
2. At most 64 KB are read.
3. A binary signature is never overridden by the extension; a mismatch is reported as the notice MISMATCH (not an error).
4. An unrecognised file is Category other and no conversion is offered.
5. For every Format there are samples: valid, truncated, wrong extension, and one large generated file (not committed); tests assert the Format or the exact code, and were written first.
