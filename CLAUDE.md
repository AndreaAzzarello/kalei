# Kalei

Local, private, modular desktop file converter (Windows first, multiplatform architecture; Avalonia 12 + .NET 10). *Kalei — convert any file, privately.* Code and comments in English.

At the start of every session read the last 3 entries of `HISTORY.md`.

## Documents
- [GLOSSARY.md](GLOSSARY.md): shared vocabulary, use these terms in code, docs and UI
- [docs/PRD.md](docs/PRD.md): what, for whom, out of scope
- [docs/TRD.md](docs/TRD.md): stack, engines, licences
- [docs/conversion-matrix.md](docs/conversion-matrix.md): supported pairs per Module
- [docs/app-flow.md](docs/app-flow.md): screens and states
- [docs/design-brief.md](docs/design-brief.md): palette, type, icon, motion
- [docs/architecture.md](docs/architecture.md): Core, Modules, Workers, IPC
- [docs/adr/](docs/adr/): architecture decisions

## Commands
Run through the Bash tool without full paths: `dotnet build`, `dotnet test`. Lint/format: `dotnet format --verify-no-changes`. (Solution does not exist yet; created in Ticket 0/1.)

## Security requirements (non-negotiable)
The only place they are written is [docs/architecture.md](docs/architecture.md#security-requirements-normative-the-only-place-they-are-written) (S1–S12). Do not copy their text elsewhere; use this checklist:
- S1 one Worker per Conversion · S2 limits and bomb protection · S3 type from content, Core reads headers only, parsers run in Workers · S4 safe output names, no overwrite, Source untouched
- S5 **release gate**: no network for Workers, proved per OS (otherwise the promise is removed for that OS) · S6 no automatic update or check · S7 temp files only in Kalei's folder (two declared exceptions) · S8 module integrity (from 1.1)
- S9 safe parsers and writers (XML, YAML, HTML, CSV, XLSX) · S10 logs without paths · S11 mouse-only hook (1.2) · S12 browser drops, no link downloads, origin mark without URLs
Zero telemetry.

## Rules
- Workers receive handles, never paths (ADR 0003). No temp copies of Source files, except the spooled browser files declared in S7/S12.
- Libraries: only permissive or LGPL (dynamic) in-process; GPL engines only as separate processes. Never add AGPL or commercial-licence libraries (QuestPDF, iText, MuPDF, Ghostscript, PDFsharp 6.5 or later…); PDFsharp 6.2.x (MIT) only as the documented, version-pinned fallback in the TRD. Build and packaging tools (e.g. the installer builder) are checked separately from shipped libraries. Check licence, trimming compatibility and size before adding a dependency, record it in docs/TRD.md.
- Do not add cookies, analytics, or any outbound network call.
- Free services only; ask before anything that costs money.
- Test first (`/tdd`). Samples in `tests/samples/` for every Format, including corrupt, huge (generated, not committed) and wrong-extension files; test both result and failure behaviour.
- One commit per step with a clear message; add a dated entry to `HISTORY.md` at every commit.
- Push, repository creation and releases only after explicit user confirmation.
- Close any dev server or process you start.
- Graphify: do not generate the graph yet. When the project exceeds a few thousand lines run `graphify . --code-only`.

## Design
Read docs/design-brief.md before touching UI. Accent follows the file Category; keep WCAG AA contrast; respect Windows "Reduce animations".
