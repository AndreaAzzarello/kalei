# History

Dated entries, newest first, 5–8 lines each, updated at every commit. No sensitive data or personal paths.

## 2026-11-10 #10 — Reference machines and Baton note
- Reference machines recorded: Hyper-V Windows 10 22H2 (2 vCPU, 4 GB) as the worst case, owner's Windows 11 desktop (two monitors) for Donut and DPI, Mac mini M1 for macOS, Ubuntu and Fedora VMs for Linux.
- W12–W15 (global hook, Sentinel, false triggers, UIPI) moved out of the 1.0 blocking proofs into a 1.2 spike.
- ADR 0001 now says what Baton is (the author's earlier WPF download manager).

## 2026-11-10 #9 — Brief, TRD and terminology cleanup
- design-brief: decisions first, options not chosen moved to an appendix; accent steps now 120° (Bloom), Donut faces table, accents marked used/reserved.
- TRD: one font-subset definition, BMP/ICO codec row, SignPath and Inno Setup checks moved before 1.0. Earlier corrections kept here: NetVips.Native is LGPL-3.0+; PDFsharp MIT only up to 6.2.x; resvg is Apache-2.0 OR MIT; only BtbN has an LGPL FFmpeg build; libvips web vs all build to check.
- Worker executable renamed Kalei.Worker.Builtin; Module/Worker/Worker executable defined in the glossary; links between documents checked (no broken link or anchor).

## 2026-11-10 #8 — Ticket 0 plan and spec drafts
- docs/ticket-0.md: 18 Windows proofs (blocking 1.0) with numeric pass/fail and fallbacks, plus Linux and macOS spikes (not blocking).
- docs/ipc-spec.md: inherited anonymous pipes, framed JSON, messages including Probe/Preview, watchdog, error codes.
- docs/module-manifest.md: manifest schema with typed Options, no limits and no hashes inside.
- docs/format-detection.md: signature table, header stage in the Core (64 KB), probe stage in a Worker.

## 2026-11-10 #7 — Multiplatform architecture, Windows first
- ADR 0010: no OS API in the Core; Platform adapters per OS; 1.0 Windows only, Linux and macOS in 1.x, each after its own spike and with the no-network gate.
- Sandbox proposed per OS with primary sources: Windows AppContainer; Linux seccomp + Landlock (bubblewrap/Flatpak as extra layer); macOS App Sandbox (works ad-hoc signed per Apple DTS; network block still to prove).
- macOS plan B without an Apple account: ad-hoc build, Open Anyway, same sandbox; notarisation is distribution only.
- Capability table per OS added to architecture; future distribution options added to the TRD; glossary: Platform adapter, Capability table.

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
