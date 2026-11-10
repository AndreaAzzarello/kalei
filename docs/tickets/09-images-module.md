# 09 — Images Module: the confirmed set from Core to file

- **Status**: ready-for-agent
- **Size**: L
- **Blocked by**: [02](02-image-engine-content.md), [06](06-header-detection-images.md), [07](07-launcher-and-worker-ipc.md), [08](08-broker-and-output-writing.md)
- **Rules**: S2, S3, S5
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
A user-visible capability through the Core API: convert between PNG, JPG, WebP, GIF (first frame) and TIFF, inside the sandbox, with the options that matter and safe failure on bad files.

## Acceptance criteria
1. For every pair in the confirmed set among PNG, JPG, WebP, GIF and TIFF the Output is valid and is read back by an independent decoder in the test.
2. Options work: quality, maximum size, strip all metadata; GPS is removed by default; orientation is applied to the pixels.
3. An animated input gives its first frame and a FIRST_FRAME_ONLY warning; transparency going to JPG is flattened onto white.
4. The pixel limit is checked from the header before decoding (E_PIXELS); a malformed file gives E_CORRUPT and no partial Output.
5. Untrusted loaders are blocked; the Conversion runs under the sandbox through the Core API end to end.
6. The Module is loaded from its manifest by the same loader that Optional modules will use.
