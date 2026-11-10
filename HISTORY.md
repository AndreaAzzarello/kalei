# History

Dated entries, newest first, 5–8 lines each, updated at every commit. No sensitive data or personal paths.

## 2026-11-10 #24 — Build spike (ticket 03a, W7–W9)
- Native AOT could not be tried: no MSVC linker on the dev PC (owner decision: not installed). It will be tried on a GitHub Actions Windows runner when CI exists; ADR 0001 stays unchanged until then. ReadyToRun + trimming works: window start-up median 418 ms warm, app 23.5 MB zipped (Windows 11 dev PC, to repeat cold on the VM, #47).
- W9: the trimmed Worker passes with warnings as errors and a smoke test on the trimmed binary. Replacements: MiniExcel (3 trim warnings) by Sylvan.Data.Excel reader + own XLSX writer; YamlDotNet only through `YamlStream`. CSV library is Sep.
- W8 first estimate: app + Worker 42.7 MB zipped (target 45). Spike code stays on branch `spike/03a-build`.

## 2026-11-10 #23 — Network canary (S5) and the firewall dependency
- Microsoft's pages (WFP sublayer "Windows Application Isolation", "UWP default" and "AppContainer loopback" block filters, BFE) show the AppContainer network block is done by WFP filters; none says what happens with the Windows Firewall service or BFE stopped, profiles off, or an allow rule. ADR 0003 now says so, with the quotes.
- New: architecture section "Network canary" (three fixed probes, verdicts, when it runs, at most 1.5 s, promise shown only if `isolated`, log event without personal data), ipc-spec messages `Canary` and `CanaryResult` and notice `NETWORK_ISOLATION_UNPROVEN`, glossary term, proofs W19 (VM, five firewall states) and W20 (canary notices it), ticket 07c (issue #50). The two external probes need the owner's confirmation before they ship.
- ADR 0003 and 0006 read "accepted per Windows 11; Windows 10 22H2 pending #47". New ticket 38 (issue #51): Native AOT trial on a GitHub Actions Windows runner, with the analysis of the ILCompiler advisory first. Tickets are now 46.

## 2026-11-10 #22 — Sandbox spike (ticket 01, W1–W4)
- On the owner's Windows 11 PC (to repeat on the reference VM, issue #47): AppContainer with no capabilities + Job Object + inherited handles converts PNG to AVIF and WebP; TCP, UDP, DNS and loopback all fail inside, all succeed outside (S5 holds here); filesystem by path denied; handles are seekable (TIFF, AVIF); memory cap, timeout, no child process and kill-on-close work; the install-folder ACL for ALL APPLICATION PACKAGES needs no admin.
- ADR 0003 and ADR 0006 now accepted for Windows 11; Windows 10 22H2, cold timings, a stopped firewall service and HEIC through WIC in the sandbox not verified. Spike code stays on branch `spike/01-sandbox`.

## 2026-11-10 #21 — Ticket 03b criterion 4 marked superseded
- Criterion 4 (original Mayenne Sans licence) now carries a "superseded" note: owner decision, only Bricolage Grotesque (OFL 1.1) is cited, Mayenne Sans is not needed. Issue #4 stays closed.

## 2026-11-10 #20 — Image engine content (ticket 02, W5 and W6)
- Prebuilt libvips 8.18.7 writes AVIF (AV1) and GIF, plus WebP and TIFF; it has no BMP, ICO or HEVC. SVG loading can be disabled and BlockUntrusted works (checked by behaviour).
- HEIC: WIC decoded a HEIC on the owner's Windows 11 PC, libvips could not. Required extensions, decode in the sandbox and Windows 10 not verified.
- Matrix: BMP, ICO and HEIC leave 1.0 until tickets 17 and 18 (confirmed by the owner). Spike code stays on branch `spike/02-image-engine`, only results are in main.

## 2026-11-10 #19 — Licence and signing checks (ticket 03b, W18)
- Inno Setup 7.1.1: licence permits use for any purpose at no cost; the site only asks commercial users to pay. Suitable.
- SignPath Foundation: free for OSS but needs a public release, MFA, manual approval per release and a code signing policy page; the Foundation is the publisher. Owner decision: applies after the first public release, 1.0 may ship unsigned; requirements added to the TRD and ticket 32. Apply form and supported CI not verified.
- Owner decision: the installer counts as binary redistribution, so Inno Setup's copyright and web address go into THIRD-PARTY-NOTICES (ticket 30).
- Mayenne Sans original OFL.txt not found and not needed: only Bricolage Grotesque (OFL 1.1) is cited, no mention in README or notices.

## 2026-11-10 #18 — Tickets published as issues
- The 44 tickets are GitHub issues #1–#44 in ticket order, with labels for status, size and group; "Blocked by" and the links between tickets use real issue numbers, links to the spec and feasibility document are absolute.
- docs/tickets/README.md now lists the issue number next to each ticket file.

## 2026-11-10 #17 — Public repository and issue tracker setup
- Commit authors rewritten to the GitHub noreply address before the first push; the public repository holds main only (the prototype branch stays local).
- Private vulnerability reporting enabled; GitHub Issues chosen as tracker, with labels for triage roles, status, size and group (docs/agents/).
- The 44 tickets are not published as issues yet.

## 2026-11-10 #16 — Raw-pixel previews and term clean-up
- Previews travel as raw BGRA pixels (length checked as width x height x 4), so the app has no image decoder and S3 has no exception; preview size added to Limits.
- "Ticket 0" in ADRs, TRD, PRD, matrix and architecture now says "the feasibility spike"; CLAUDE.md index lists spec and tickets.

## 2026-11-10 #15 — Repository setup files
- Added LICENSE (MIT), README (English with an Italian section; every claim marked planned or conditional on the S5 release gate), SECURITY.md (private vulnerability reporting, scope, SHA256SUMS check) and .gitignore (.NET, Avalonia, backups, generated samples).
- Branch renamed master to main (local only; no remote, no push).
- GitHub Actions free plan for public repositories with standard runners checked on the primary source and recorded in the TRD and the release pipeline; Windows runners are not named on that page, so the first run is checked on the billing page.

## 2026-11-10 #14 — Review fixes to spec and tickets
- Five reviewers read spec, tickets and the feasibility document with empty context; every finding was checked against the text (none false; three partial). The owner approved all fixes and eight product choices.
- Product choices: XLSX "all sheets" moves to 1.1; no back Face or wheel flip in 1.0; no saved conversion defaults; no "Treat as…"; the releases button lives in Settings as "Open Releases page"; Cancel is cooperative with a kill after 3 s; watchdog 10 s with progress every 2 s; UTF-16 accepted as input.
- Batch rule is now by Format (a file already in the target is skipped); TXT → TXT is the only same-Format pair; limits table gained editable and hard-maximum columns; notices are separate from error codes.
- Tickets regenerated: 44 (03, 07, 09, 10, 12, 14, 20 split into a and b, new ticket 37 for the Advanced panel); each has stories, manual-check line and numbered criteria; the README has a story-to-ticket table.

## 2026-11-10 #13 — Tickets for 1.0
- docs/tickets: 36 local tickets as vertical slices with blocking edges, size, rules S1–S12 and numbered acceptance criteria; index with starting points and critical path.
- Groups: Windows spike 01–04, core and first flow 05–12, rest of 1.0 13–29, release 30–33, non-blocking spikes 34–36 (global gesture, Linux, macOS).
- Checked: no dependency cycles, every S rule and every W/L/M proof assigned to a ticket, links and anchors valid.

## 2026-11-10 #12 — Settings and release pipeline specs
- docs/spec/settings.md: data locations (installed and portable), settings schema with atomic write, migration and corrupt-file handling, Limits page rules, localisation rules, error catalogue test, log format and rotation.
- docs/spec/release-pipeline.md: build to draft release in order, sandbox set-up at install time, portable behaviour, uninstall, CI cost check.

## 2026-11-10 #11 — 1.0 Windows spec
- docs/spec/1.0-windows.md: problem, solution, 65 user stories, implementation decisions (components, formats and defaults, preselected targets, Batch rules), three test seams, out of scope.
- Test seams: Core pipeline through the sandbox-launcher interface, the Worker IPC boundary, the pure Donut library.

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
