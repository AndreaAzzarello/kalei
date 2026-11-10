# History

Dated entries, newest first, 5–8 lines each, updated at every commit. No sensitive data or personal paths.

## 2026-11-10 #6 — Security: one source of truth, no-network release gate
- architecture.md now holds the only copy of the security rules (S1–S12); CLAUDE.md keeps a one-line checklist with the same numbers.
- S5: no network for Workers is a release gate per OS; if not proved, the promise is removed, no fallback is sold as equivalent.
- New rules: Core reads headers only (64 KB), previews/thumbnails/structure detection in a Worker; per-file module hashes at every load; limits never from module.json.
- Output hygiene: CSV formula neutralising, HTML CSP, explicit XLSX strings, temp-file exception with journal, origin mark without URLs.
- Limits table, inherited anonymous pipes instead of named pipes, PDFsharp rule made precise (6.5+ banned, 6.2.x pinned fallback).

## 2026-11-10 #5 — Narrow 1.0 scope after the EGM review
- Review by five fresh reviewers gave 24 findings; all accepted and checked against the real text (none false).
- 1.0 now: Windows 10 22H2/11 x64, in-app Donut kept, only formats confirmed by the spike, one target format per Batch, no size Estimate, no module installer.
- Module installer, per-Category targets, Explorer menu, CLI and recent conversions move to 1.1; Linux/macOS to 1.x; global Donut stays 1.2.
- Formats now map to one Category each (JSON/YAML/XML count as document); PRD Release order is the single source for version numbers.

## 2026-11-10 — Icon contrast fix and .ico
- App icon container changed from #4B3FD1 (2.27:1 on a dark taskbar) to #6C5FF1 (worst case 3.06:1 over a taskbar sweep; petals 4.62:1).
- The accent dot now sits on a container-coloured halo (it had only 1.68:1 against white petals).
- Added assets/icon: two SVG sources, kalei.ico (16/24/32/48/256) and its build script.
- PDF/hook/font research recorded in the TRD (SKDocument does not subset fonts; Mayenne Sans licence still unverified).

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
