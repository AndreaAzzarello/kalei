# 09a — Images Module core: Worker host, manifest, PNG, JPG and WebP from Core to file

- **Status**: ready-for-agent
- **Size**: L
- **Blocked by**: [02](02-image-engine-content.md), [06](06-header-detection-images.md), [07a](07a-ipc-protocol-and-worker-host.md), [07b](07b-windows-sandbox-launcher.md), [08](08-broker-and-output-writing.md)
- **Rules**: S2, S3, S5, S8
- **Stories**: 41, 43 ([user stories](../spec/1.0-windows.md#user-stories))
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
The first working vertical slice through the Core API: convert between PNG, JPG and WebP inside the sandbox, with the built-in Worker executable and the Module manifest loader in place, and fail safely on bad files.

## Acceptance criteria
1. The built-in Worker executable hosts the images Module; the Core's Module manager reads its manifest with the loader that Optional modules will also use; a manifest with a limits field or an unknown field is rejected (test).
2. Every pair among PNG, JPG and WebP (as confirmed by ticket [02](02-image-engine-content.md)) gives a valid Output file, read back by an independent decoder: Windows WIC where it supports the Format, otherwise a test-only second library named in the ticket after a licence check recorded in the TRD.
3. The pixel limit is checked from the header before decoding (E_PIXELS); a malformed file gives E_CORRUPT and no partial Output file; untrusted loaders are blocked.
4. The Conversion runs under the sandbox through the Core API end to end; Probe and Preview answer for images from the header and a bounded decode.
5. A generated image at the pixel limit converts within the memory and time limits on the main reference machine, or the Limits table is changed with the measurement recorded.
