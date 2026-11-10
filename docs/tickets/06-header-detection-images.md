# 06 — Format detection from the header for images

- **Status**: ready-for-agent
- **Size**: S
- **Blocked by**: [05](05-solution-skeleton.md)
- **Rules**: S3
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
The Core recognises the real image Format of a file from its first bytes, ignores a lying extension, and says "not recognised" for anything else.

## Acceptance criteria
1. Signatures of PNG, JPG, GIF, WebP, BMP, TIFF, ICO and the AVIF/HEIC brands are detected as in the format detection document.
2. At most 64 KB are read.
3. A binary signature is never overridden by the extension; a mismatch is reported as an informational E_MISMATCH.
4. An unrecognised file is Category other and no conversion is offered.
5. For every Format there are samples: valid, truncated, wrong extension; tests assert the Format or the exact error code, and were written first.
