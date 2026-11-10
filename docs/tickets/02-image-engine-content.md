# 02 — Image engine content: libvips build, BMP and ICO, HEIC through WIC

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: –
- **Rules**: S3
- **Stories**: none (constraint, spike or infrastructure)
- **Proofs**: W5, W6 ([ticket-0](../ticket-0.md))
- **Spec**: [ticket-0, Part A](../ticket-0.md#part-a--windows-blocks-10)

## What to build
Find out, by running it, exactly which image Formats the prebuilt libvips package can read and write, and whether HEIC can be decoded through WIC (the Windows Imaging Component), so the 1.0 image matrix lists only what really works.

## Acceptance criteria
1. W6: the loaders and savers present in the prebuilt libvips package are listed; AVIF writing, GIF writing, TIFF and WebP are each recorded as present or absent.
2. Untrusted loaders can be blocked, and the SVG loader can be disabled; both are shown working.
3. BMP and ICO: whether the engine handles them is recorded; if not, the decision to write own codecs (ticket [17](17-images-bmp-ico.md)) is recorded.
4. W5: HEIC decoding through WIC is tried outside the sandbox, with the list of Windows extensions actually required; the same inside the sandbox once the ticket [01](01-sandbox-spike.md) harness exists, otherwise noted for ticket [18](18-images-heic-via-wic.md).
5. The confirmed image Format set is written into the conversion matrix as the 1.0 list; every unconfirmed Format is marked as leaving 1.0.
