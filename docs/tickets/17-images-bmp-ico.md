# 17 — Images: BMP and ICO codecs

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [02](02-image-engine-content.md), [09](09-images-module.md)
- **Rules**: S2, S3
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
BMP and ICO convert correctly, using own small codecs inside the Worker if the engine does not provide them.

## Acceptance criteria
1. BMP and ICO read and write work, from the engine or from own codecs as ticket 02 decided.
2. ICO output holds 16, 32, 48 and 256 px images, never upscaled; a larger input is scaled down; as input the largest image is used.
3. Fuzz tests (malformed headers, huge dimensions, truncation) end in E_CORRUPT or E_PIXELS, without a crash or hang beyond the Limits.
4. Round trips give the expected images; everything runs under the sandbox.
