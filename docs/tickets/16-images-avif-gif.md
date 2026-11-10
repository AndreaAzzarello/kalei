# 16 — Images: AVIF and GIF writing

- **Status**: ready-for-agent
- **Size**: S
- **Blocked by**: [02](02-image-engine-content.md), [09](09-images-module.md)
- **Rules**: S2
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
AVIF and GIF output join the image set, if ticket 02 confirmed that the engine can write them.

## Acceptance criteria
1. If ticket 02 confirmed them, AVIF reading and writing and GIF writing work with valid, independently readable output; otherwise this ticket is closed as "not in 1.0" and the matrix is updated.
2. Pixel and size limits are respected.
3. Samples (valid, truncated, wrong extension) are covered by tests.
