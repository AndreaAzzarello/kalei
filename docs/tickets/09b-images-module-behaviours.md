# 09b — Images Module behaviours: GIF as source, TIFF, options, metadata, flattening

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [09a](09a-images-module-core.md)
- **Rules**: S2, S3
- **Stories**: 32, 33, 34 ([user stories](../spec/1.0-windows.md#user-stories))
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
The image behaviours the user can see: more formats, the options that matter, and privacy-friendly metadata handling.

## Acceptance criteria
1. TIFF reads and writes with PNG, JPG and WebP; GIF is read as a source only (first frame) and converts to each of them; GIF as a target is ticket [16](16-images-avif-gif.md).
2. Options work: quality, maximum size, strip all metadata; GPS is removed by default; orientation is applied to the pixels.
3. An animated input gives its first frame and the notice FIRST_FRAME_ONLY; transparency going to JPG is flattened onto white.
4. Every Output file is read back by the independent decoder of ticket [09a](09a-images-module-core.md).
