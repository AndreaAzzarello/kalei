# 16 — Images: AVIF, and GIF as a target

- **Status**: ready-for-agent
- **Size**: S
- **Blocked by**: [02](02-image-engine-content.md), [09b](09b-images-module-behaviours.md)
- **Rules**: S2
- **Stories**: none (constraint, spike or infrastructure)
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
AVIF read and write, and GIF as a target, join the image set, if ticket [02](02-image-engine-content.md) confirmed that the engine can do them.

## Acceptance criteria
1. If ticket [02](02-image-engine-content.md) confirmed them, AVIF reading and writing and GIF writing work with valid, independently readable Output files; otherwise this ticket is closed as "not in 1.0" and the matrix is updated.
2. The pixel and size limits are respected.
3. Samples (valid, truncated, wrong extension) are covered by tests.
