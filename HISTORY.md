# History

Dated entries, newest first, 5–8 lines each, updated at every commit. No sensitive data or personal paths.

## 2026-11-10 — Round 4 decisions recorded
- After studying Filee (read only, nothing copied): Kalei stays independent (ADR 0007).
- Donut in 1.0 as an in-app overlay (ADR 0008); global Donut in 1.2, opt-in, signed build (ADR 0009).
- Global gesture: mouse hook only, modifier polled, no keystroke recording; minimal Sentinel process (≤ 20 MB target, to measure).
- Release order: 1.0 window + in-app Donut, 1.1 Explorer menu + CLI, 1.2 global Donut. Layout A Quiet confirmed.
- Ticket 0 spike extended: in-app Donut, Sentinel RAM, Native AOT with Avalonia + Skia.

## 2026-11-10 — Brand choices and prototype
- Chosen: logotype "kalei" in Bricolage Grotesque (OFL 1.1, embedded subset); icon Bloom (bare in app, in rounded container for app icon), centre dot follows the category accent.
- PDFsharp: MIT only up to 6.2.x, AGPL/commercial from 6.5.0; latest stable stays excluded (see TRD).
- Prototype (variants Quiet/Prism + icon sheet) lives on branch `prototype/ui-variants`; screenshots in `prototype-screenshots/`.
- Layout A vs B still to be chosen.

## 2026-11-10 — Phase 1 documents
- Added GLOSSARY.md and ADRs 0001–0006 (stack, module = process, sandbox, module hashes, licensing, libvips).
- Added PRD, app flow, architecture, design brief (palette contrast verified), TRD, conversion matrix.
- Licence research found: PDFsharp is AGPL (rejected), NetVips.Native is LGPL-3+, gyan.dev FFmpeg is GPL-only (use BtbN LGPL build).
- Added CLAUDE.md with security requirements. No app code yet.
- Next: UI prototype (two variants), EGM review, spec and tickets.
