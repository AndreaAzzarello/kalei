# 18 — Images: HEIC reading through WIC

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [02](02-image-engine-content.md), [09](09-images-module.md)
- **Rules**: S3, S5
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
A user with the right Windows extensions can convert a HEIC photo; otherwise Kalei says exactly what is missing.

## Acceptance criteria
1. If W5 passed, HEIC is read through the WIC adapter inside the sandbox; otherwise this ticket is closed as "not in 1.0" and the matrix is updated.
2. Without the required extension the error E_NO_CODEC names what to install.
3. No HEVC codec is shipped (ADR 0005).
4. The result works for HEIC samples with and without metadata.
