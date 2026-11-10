# Kalei

Local, private, modular desktop file converter (Windows, Avalonia 12 + .NET 10). *Kalei — convert any file, privately.* Code and comments in English.

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

## Security requirements (non-negotiable; see docs/architecture.md)
1. Each Conversion runs in its own Worker process with timeout, memory limit and cancel.
2. Limits on file size and count; zip-bomb and malformed/malicious file protection (including XLSX).
3. Detect the real Format from content (magic bytes), never trust the extension alone.
4. Sanitise output names and paths: no path traversal, no silent overwrite, never touch Source files.
5. No network for Workers. The only connection allowed is installing an Optional module the user approved, from the fixed source, hash verified (hash embedded in the app).
6. No automatic update without consent.
7. Temporary files only in the dedicated Kalei folder, always deleted at the end, on error and on cancel.
8. XML without DTD/external entities, YAML without arbitrary types, sanitised HTML output, no formulas written into XLSX.
9. Logs contain no paths or file names. Zero telemetry.

## Rules
- Workers receive handles, never paths (ADR 0003). No temp copies of Source files.
- Libraries: only permissive or LGPL (dynamic) in-process; GPL engines only as separate processes. Never add AGPL or commercial-licence libraries (QuestPDF, iText, PDFsharp, MuPDF, Ghostscript…). Check licence, trimming compatibility and size before adding a dependency, record it in docs/TRD.md.
- Do not add cookies, analytics, or any outbound network call.
- Free services only; ask before anything that costs money.
- Test first (`/tdd`). Samples in `tests/samples/` for every Format, including corrupt, huge (generated, not committed) and wrong-extension files; test both result and failure behaviour.
- One commit per step with a clear message; add a dated entry to `HISTORY.md` at every commit.
- Push, repository creation and releases only after explicit user confirmation.
- Close any dev server or process you start.
- Graphify: do not generate the graph yet. When the project exceeds a few thousand lines run `graphify . --code-only`.

## Design
Read docs/design-brief.md before touching UI. Accent follows the file Category; keep WCAG AA contrast; respect Windows "Reduce animations".
