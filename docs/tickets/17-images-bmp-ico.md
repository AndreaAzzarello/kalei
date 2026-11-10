# 17 — Images: BMP and ICO codecs

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [02](02-image-engine-content.md), [09b](09b-images-module-behaviours.md)
- **Rules**: S2, S3
- **Stories**: none (constraint, spike or infrastructure)
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
BMP and ICO convert correctly, using own small codecs inside the Worker if the engine does not provide them.

## Acceptance criteria
1. BMP and ICO read and write work, from the engine or from own codecs as ticket [02](02-image-engine-content.md) decided.
2. ICO output holds 16, 32, 48 and 256 px images, never upscaled; an image smaller than 16 px is stored once at its own size; a larger input is scaled down; as input the largest image is used.
3. Transparency going to BMP is flattened onto white.
4. Fuzz tests (malformed headers, huge dimensions, truncation) run at least 10,000 mutated inputs per codec per CI run with fixed seeds, plus 10 minutes of open-ended fuzzing before release; every input ends in E_CORRUPT or E_PIXELS, or converts, without a crash or a hang beyond the Limits.
5. Round trips give the expected images; everything runs under the sandbox.
