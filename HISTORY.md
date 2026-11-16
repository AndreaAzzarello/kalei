# History

Dated entries, newest first, 5–8 lines each, updated at every commit. No sensitive data or personal paths.

## 2026-11-16 #67 — Ticket 06, step 2: reader, MISMATCH, robustness tests and samples
- `HeaderReader.Read` stops at 64 KB (tested on a synthetic 200 GB stream: exactly 65,536 bytes read; partial reads and empty streams handled). `FormatDetection.Classify` gives Format, Category (`image` or `other`) and the MISMATCH flag; the extension never changes the Format.
- Fuzz: 10,000 inputs, seed `0x060006`, length 0 to 70,000, half with a mutated signature and extreme `ftyp` sizes: no exception, result always one of the nine ids or null. Same for empty, 1-byte, 64 KB, 64 KB + 1 and 1 MB inputs.
- Samples in `tests/samples/<format>/` (valid, truncated, wrong extension; all under 650 bytes, generated with Pillow, HEIC header hand-made); `.gitattributes` keeps them byte-exact (`autocrlf` would otherwise touch the PNG signature). Full suite: 413 tests green.

## 2026-11-16 #67 — Ticket 06, step 1: header detector and the ftyp brand rule
- `HeaderDetector.Detect` (Core, pure, allocation-free) recognises PNG, JPG, GIF, WebP, BMP, TIFF, ICO, AVIF and HEIC from at most 64 KB; everything else is unknown. Tests first: with `Detect` stubbed to `null`, 44 of the 87 detection tests failed; with the real code all 87 pass.
- New rule in `format-detection.md`, deterministic: a known major brand (`avif`/`avis`, `heic`/`heix`/`hevc`/`hevx`) decides; only `mif1`/`msf1` read the compatible brands, and if both `avif` and `heic` are there **AVIF wins** (confirmed in 1.0, readable and writable by the engine; HEIC is input-only and out of 1.0). `avis` is a major brand only, `heis` is not in the table (owner confirmed).
- Hostile box sizes (0, 1 with 64-bit size, too small, beyond the data) are covered by tests.

## 2026-11-15 #57 — Ticket 07b: VM report, handshake timeout 15 s, results in the documents
- Report of the kit on the Windows 10 22H2 VM: 20 of 20 sandbox proofs pass; cold `Hello` after `Start`: 8 of 10 between 634 and 796 ms (median 720 ms), but two consecutive starts at 4,835 and 5,387 ms with `Start` itself ten times slower (the whole VM was slow after those reboots). The 5 s handshake would have failed the second one.
- Owner decision: **handshake timeout 15 s** (`WatchdogTimes.Hello`, test first; heartbeat 10 s, request timeouts and the 3 s after `Cancel` unchanged). Reason: the Worker is already confined, so a longer wait costs no safety, and 15 s is 2.8 times the worst case measured. Future option, not now: a ReadyToRun Worker (to be measured with the engines, ticket 09a).
- Documents: `ticket-0` (W16 Worker part and the cold `Hello` table), ADR 0003 (launcher verified on Windows 11 and 10 22H2), `ipc-spec` (Watchdog and the margin), `architecture` (how the Worker limits are enforced: job CPU cap as a safety net, `E_MEM` deduced from the peak), `release-pipeline` (`--remove-sandbox`, narrower grant). Not verified: firewall-off states (07c, #47), native allocation at the cap (09a), the journal half of W16 (08).

## 2026-11-15 #56 — Ticket 07b: first CI run, folder owner of an elevated administrator
- The first CI run failed in every sandbox test: on the runner the folders belong to the Administrators group (the token's default owner), so the set-up answered `FolderNotOwned`. This was one of the "low" notes of the audit; it is a real defect, so it is fixed: a folder counts as the user's if its owner is the user or the token's default owner. Ticket 07b's known limit is reworded. Not provable locally (this PC's token owner is the user); the CI is the check.

## 2026-11-15 #55 — Ticket 07b: known limits and an installer criterion
- Ticket 31 (issue #38) gets criterion 8: the clean-up must not remove a folder access the installer created (shared marker, or remove only what the app added). Ticket 07b gets a "Known limits" section with the other two low findings of the audit (pipe-handle inheritance window, Administrators-owned folder). No code change.

## 2026-11-14 #54 — Ticket 07b, step 8: security audit of the IPC and the launcher, one fix
- Focused review (not the full six-phase workflow) of `src/Kalei.Core/Ipc`, `ISandboxLauncher` and `src/Kalei.Platform.Windows/Sandbox`, threat model: the Worker is untrusted, the Core is trusted. IPC framing, caps, validation, watchdog and session: no confirmed finding. Launcher: job limits, handle list, environment and kill-on-close as designed.
- **Medium, fixed:** the first-run grant to ALL APPLICATION PACKAGES was inherited by every subfolder, so in portable mode a compromised Worker could read `data\` (settings, logs) and any folder placed next to the executable. Now the grant is "this folder and the files directly in it" (`ObjectInherit` + `NoPropagateInherit`); the built-in Worker has no subfolders. Test first (red, then green); the real-process sandbox tests still pass. A grant already made by an installer is still accepted.
- Left as notes (low): `Remove()` deletes every explicit AAP allow rule on the folder, also one an installer made; the inheritance window of the pipe handles is closed only against other Kalei launches, not against another `CreateProcess` in the host; `FolderNotOwned` when the folder is owned by the Administrators group.

## 2026-11-14 #53 — Ticket 07b, step 6: SandboxCheck tool and the kit for the reference VM
- `tests/Kalei.SandboxCheck` (test-only, never shipped) now has `hello` (one start of the Worker, timed: set-up, `Start`, launcher-returns-to-Hello, total), `proofs` (the 20 sandbox proofs of the tests, each with its control outside the sandbox, PASS / FAIL / INCONCLUSIVE, for a machine with no SDK), `remove` and `host`. Run here on the Windows 11 PC from the tool: 20 of 20 PASS; warm Hello 182.6 ms after `Start` (233 ms in total, `Start` itself 50 ms).
- Kit in `tests/Kalei.SandboxCheck/kit/` (sources) and built into `E:\KaleiVM-07b\` by `build-kit.ps1` (97 MB, the Worker published trimmed as CI does, tools and test Worker untrimmed in their own folder; never touches `stato\` or `risultati\`). `LEGGIMI.txt` in Italian. Order: environment, first start after the copy (set-up of the sandbox included), 20 proofs, 10 cold starts (reboot, 150 s settle, one start), report. No automatic sign-in, UAC or secret (the VM signs in without a password). State written before each step that can fail or reboot, data only appended, report sanitised (not written if a user name, computer name or personal path is left) and copied to `\\tsclient\E` if the enhanced session is on, otherwise the manual copy is explained. The report says whether the worst case passes 2.5 s (both "after Start", which is what the 5 s timer measures, and "total").
- Tried on the PC with Windows PowerShell 5.1, without rebooting: a dry run, and a simulated sequence (`-NoReboot`, `-FakeUptime`, `-SettleSeconds`: first run, resume after a reboot, manual rerun while the reboot never happened, resume, finish): state, RunOnce removal, report and clean-up (profile and folder access gone) behave; those runs are marked "not valid" in the data. A bug found there: Windows PowerShell 5.1 hands a JSON array to the pipeline as one object (the proofs counted as one); fixed with `| ForEach-Object { $_ }`. **Not verified: the real reboot and the RunOnce resume on the VM** (they need the VM).

## 2026-11-14 #52 — Ticket 07b, step 5: clean-up command and first-run set-up
- `Kalei.App.exe --remove-sandbox` (`SandboxCommandLine`, Kalei.Platform.Windows): removes the AppContainer profile and the folder access, opens no window, exit code 0 or 1; any other arguments start the app as before. Windows only (`OperatingSystem.IsWindows()`). The first-run set-up is the launcher's: its first Start runs `SandboxSetup.Prepare()` once; anything but Ready gives `E_SANDBOX` and starts nothing (tested in step 4). The wiring of the launcher into the main window is ticket 10b.
- Tests first (5, all pass; 297 in the solution). The published App (ReadyToRun + trimmed) was run with the argument: exit 0. A first publish crashed with FileNotFoundException for `Kalei.Platform.Windows`: the incremental trim cache of the local `obj` was older than the code that now uses the assembly (its `deps.json` did not list it); after deleting `bin` and `obj` it works, as on a clean CI checkout. So that this cannot come back unseen, `ci.yml` gets a smoke step that runs the published App with `--remove-sandbox` and requires exit 0. **The CI change can only be tried by running CI.**

## 2026-11-14 #51 — Ticket 07b, step 4: tests on the real process in the sandbox
- `tests/Kalei.FakeWorker` gains probes (`probe:<name>`: TCP, raw UDP DNS, DNS lookup, read and write by path, read through an inherited handle, child process, environment) and two resource misbehaviours that exist only as real processes (`AllocateForever`, `SpinAfterHandshake`). `tests/Kalei.SandboxCheck` (test-only exe) is the separate "launcher" process for the kill-on-close tests. Every "must be blocked" test first runs the same probe outside the sandbox and fails as INCONCLUSIVE if that control does not succeed; the three that need the Internet for their control are skipped, not passed, when it is missing (none skipped on the Windows 11 PC).
- On the Windows 11 PC (Windows 10.0.26300): TCP to a local listener, TCP to the Internet, a UDP DNS query and a DNS lookup are blocked in the sandbox and work outside; a file by path, the user profile, the temp folder and the install folder (write) are denied; a listed handle is read and seeks, an unlisted one is not inherited; a child process is refused; the Worker has no user variables; a hung Worker (no heartbeat) is killed within 12 s of its start (10 s of silence); Cancel ends a Worker that ignores it; the Worker is gone within 5 s of its launcher being killed and of its launcher ending normally; a missing Worker or a missing set-up gives `E_SANDBOX`, is logged, and starts nothing.
- Measured, and changed the design: the memory cap does not post a job message for a .NET Worker, because the GC reads the job's cap and throws the OutOfMemoryException itself at 75% of it (the spike's Worker stopped at 72% of 2 GB). The cause is therefore read from the job's peak memory (at least 70% of the cap after the Worker ended: `E_MEM`), plus the job message for a native allocation that fails at the cap. A wrong constant (the "end of job time" message is 1, not 4) was caught by the memory test.
- 292 tests in the solution, all pass. Not verified: the Windows 10 reference VM (kit, step 6); a native (libvips) allocation hitting the cap, which needs the image Module (ticket 09a).

## 2026-11-14 #50 — Ticket 07b, step 3: the Windows sandbox launcher
- `WindowsSandboxLauncher` and `SandboxedWorker` (Kalei.Platform.Windows/Sandbox), written again from the spike's sequence, not copied: set-up once on the first Start (anything but Ready: no Worker, `E_SANDBOX`); two anonymous pipes created non-inheritable; the pipe ends and the Source/Output handles are made inheritable only while `CreateProcess` runs (one launch at a time) and are the only handles in `PROC_THREAD_ATTRIBUTE_HANDLE_LIST`; AppContainer with no capabilities; process created suspended, put in the Job Object (kill-on-close, one process, per-process memory cap, CPU time cap, die on unhandled exception, completion port for the cause), then resumed. Any failure ends the new process and throws `SandboxStartException` (a closed list of failures and the OS error number, never text); `WorkerSession` logs it as `SandboxStart`.
- Measured: with a minimal environment (no profile variable) `CreateProcess` fails with error 203 for an AppContainer; the Worker now gets `LOCALAPPDATA`, `TEMP`, `TMP` pointing to the Windows temp folder (which the sandbox cannot use), `PATH` (system32 only), `SystemRoot` and `windir`, and nothing of the user's environment.
- Test first: the real Kalei.Worker.Builtin in the sandbox sends its Hello and answers `E_FORMAT` (the first launcher test; 271 in the solution, all pass).

## 2026-11-14 #49 — Ticket 07b, step 2: AppContainer profile and sandbox set-up
- `Kalei.Platform.Windows/Sandbox`: `Native` (LibraryImport, so trimming and AOT safe; the SID is a `SafeHandle` freed with FreeSid), `AppContainerProfile` (no capabilities; created or taken if it exists), `SandboxSetup`: `Prepare()` creates the profile and, only if read-and-execute for ALL APPLICATION PACKAGES is missing and the folder's owner is the current user, adds that one access rule (inherited by folders and files); a folder that is not the user's and has no grant gives `FolderNotOwned` and is left untouched; `Remove()` deletes the profile and the explicit grant, and can be repeated.
- Tests first (6, all pass, on a temporary folder and a profile name of their own; no profile left behind, checked in the registry).

## 2026-11-14 #48 — Ticket 07b, step 1: the launcher seam in the Core
- `ISandboxLauncher.Start(WorkerStart)`: handles to inherit (SafeHandle, never paths), memory cap and CPU time cap. `IWorkerProcess.EndCause` (`None`, `MemoryLimit`, `TimeLimit`): when the sandbox ended the Worker for a limit and the channel closed, `WorkerSession` answers `E_MEM` or `E_TIME` instead of `E_CRASH`. `RequestOptions` carries `Handles` and `MemoryLimitBytes` (default 2 GB, the Limits table); the CPU cap is the request timeout times the logical processors, a safety net the wall-clock watchdog always beats.
- Tests first (4 new, 262 in the solution, all pass); the test launchers follow the new signature.

## 2026-11-14 #47 — Ticket 07b: criterion 6 moved to ticket 08
- W16 splits in two: ticket 07b proves the Worker half (a hung Worker killed, Cancel ends the Worker, nothing left running); ticket 08 owns the partial Output file and the journal (its criteria 3 and 4). 07b is no longer blocked by 08. Tickets 07b and 08 and the README table updated; issues #9 and #10 get the same text. Documents only.

## 2026-11-14 #46 — Ticket 07a merged (#64); criterion 6 added to ticket 09a
- PR #64 squash-merged after a green `build`, issue #8 closed. Ticket 09a (issue #11) gets criterion 6: the constant Module declarations of the Worker (images, data, text 1.0.0) are replaced by the manifests' when the engines arrive, and no Module is declared without an engine. Issue body updated with the same text. Documents only.

## 2026-11-13 #45 — Ticket 07a, step 3: Worker host, real Worker over real pipes, Hello latency
- `Kalei.Worker.Builtin`: `WorkerHost` (Hello first, before any engine; one request; pure heartbeat every second while it runs; Cancel stops it; any broken frame from the Core ends it with code 2, unanswered), `UnavailableHandler` (every request answers `E_FORMAT` until the Modules exist), `BuiltinModules` (images, data, text, 1.0.0, constants of the build). Started with two handle numbers; without them it exits 2. CI smoke step now expects exit 2.
- Tests: 258 in the solution, all pass. The Worker host in-process (Hello first, heartbeats at most 10 per second, Cancel, 8 kinds of broken Core), the real Worker executable through `WorkerSession` over anonymous pipes, and the Worker **published trimmed** by the test itself (`PublishedWorkerTests`, `-p:TreatWarningsAsErrors=true`).
- **Hello latency, published trimmed Worker, process creation to the Hello frame read, WARM on the Windows 11 PC, 10 starts after one to fill the cache: min 202 ms, median 206 ms, max 220 ms** (the spike Worker: 46 ms, a text line). Not a cold start; to repeat cold on the reference VM in ticket 07b. Against the 5 s handshake timeout: 24 times the median, 22 times the maximum.
- Checked locally like CI: `dotnet build -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`, Worker and App published trimmed with warnings as errors (no warning). Not verified: the sandbox (07b), cold start of the Worker, CI of the PR (see the PR).
- First CI run of the PR failed on one test (race: the Worker host returned on Cancel before the engine had seen the cancellation). Fix: the host waits up to 1 s for the engine to unwind; the in-process timers of the session tests were also loosened (Hello 1 s, heartbeat 500 ms; process transport heartbeat 2 s) against slow runners. Whole suite run twice in Release locally: 258 of 258.
- Second CI run: the heartbeat test counted heartbeats in a fixed half second and saw 1 on the slower runner. Rewritten to depend on events, not on speed: the handler finishes only when the test releases it, and the test checks four heartbeats arrive at least 50 ms apart (interval 100 ms).
- Third CI run: tests green; the smoke step failed because PowerShell ends a step with the exit code of the last native command (the expected 2). The step now resets `$LASTEXITCODE` after checking it. The change to the CI file can only be tested by running CI.
- Spec v1 clarified in the same PR: the size-cap row applies to known types (an unknown type or a missing `{"type":"` prefix is case 5 or 4: no cap can be chosen, the body is never read in full); `IPC_END` is logged only for channel failures.

## 2026-11-13 #44 — Ticket 07a, step 2: Core session, watchdog, fake launchers
- `WorkerSession` (Core): one request per Worker; state machine `AwaitHello` → `Open` → `Done`; watchdog (Hello 5 s, no frame 10 s, request timeout, Cancel 3 s, 3 s after the final frame); Progress checked (not decreasing, not above the Source size); answer must match the request; a frame after the final one is `OutOfSequence`; the Worker is always ended before the call returns; a launcher that throws is `E_SANDBOX`. `ISandboxLauncher`/`IWorkerProcess` defined (streams, `Kill`, `WaitForExitAsync`); `IModuleCatalog` is the seam for the manifest loader. `LogEvent` carries typed `Error`, `Reason`, `Phase` (enums only) and the new code `IpcEnd`, written only when the channel failed.
- Tests first, 83 pass: honest paths for the four requests and every row of the spec table (cases 1–15), each on two transports: in-process (in-memory pipes, timers of 300 ms) and a real process (`tests/Kalei.FakeWorker`, anonymous pipes, no sandbox). The scripted behaviours are one file shared by both (`Behaviours.cs`).

## 2026-11-13 #43 — Ticket 07a, phase 2, step 1: IPC messages and framing (branch feat/07a-ipc)
- PR #63 (spec v1) merged after a green `build`. Code in `src/Kalei.Core/Ipc`: the 12 messages as records with the discriminator `type` (System.Text.Json source generator, `UnmappedMemberHandling.Disallow`, `RespectNullableAnnotations`, strict enums), `FrameWriter`, `FrameReader` (length, cap of the type read from the first 32 bytes, strict UTF-8, `JsonDocument` with depth 4 and no duplicate keys, no nulls, unknown/missing field found from the generated metadata, ranges and patterns), `FrameRateLimiter` (100 frames per second). Tests first: 124 pass (cases 1–8 and 12 of the spec table, caps, round trips).
- Options checked on Context7 (/dotnet/docs) before writing: `JsonSourceGenerationOptions.UnmappedMemberHandling`, `JsonDocumentOptions.AllowDuplicateProperties` (.NET 10), `JsonPolymorphic` discriminator that must come first unless `AllowOutOfOrderMetadataProperties`, `RespectNullableAnnotations` (null throws, a missing property does not: `required` does that).

## 2026-11-13 #42 — Ticket 07a, phase 1: IPC specification v1 (PR, not merged)
- `docs/ipc-spec.md` is v1: every message with fields, limits and a JSON example (all examples parsed); version negotiation (exact match, `1`); framing with unsigned length, 1 MB protocol maximum, a size cap per message type (only the `PreviewData` payload reaches 1 MB) and a rate cap of 100 frames per second; session state machine; watchdog table; case table for malformed, unknown, oversized and out-of-sequence frames, each with a closed log reason and an error code. New code `E_CRASH`. Decisions D1–D9 recorded as taken by the owner.
- Handshake margin written with the measured data: the Worker's cold start was never measured (warm 46–48 ms); against the UI's cold figures on the VM the 5 s timeout is 18 times `Main` (278 ms) and 3.1 times the slowest full-window start (1,614.8 ms), an inference. Ticket 07b must measure `Hello` cold.
- `architecture.md` and `module-manifest.md` aligned (string options at most 256 bytes). Documents only, no code.

## 2026-11-13 #41 — Ruleset on main: PR and green CI required
- PR #61 (ticket 39) squash-merged, issue #52 closed. Ruleset `main` created on the default branch and active, with an empty bypass list: pull request required (0 approvals, solo owner), required status check `build`, force push and branch deletion blocked, branch not required to be up to date. Checked with `gh api` (rules listed for `main`: deletion, non_fast_forward, pull_request, required_status_checks).
- `CLAUDE.md`: no direct push to `main`, every change goes through a branch and a PR. Not tested by an actual rejected push (the agent did not push to `main` on purpose).

## 2026-11-13 #40 — Ticket 39 (issue #52): CI on GitHub Actions
- `.github/workflows/ci.yml`: on push to `main`, on pull requests and on manual dispatch; one job `build` on `windows-latest`. Steps: restore, `dotnet build -warnaserror`, `dotnet test`, `dotnet format --verify-no-changes`, Worker published trimmed with warnings as errors plus smoke test (exit 0), App published ReadyToRun + trimmed with warnings as errors, then `build/Assert-RuntimeVersion.ps1` (fails below runtime/ILCompiler 10.0.14, or if it finds none).
- Security: actions pinned to full SHA (checkout v7.0.1, setup-dotnet v6.1.0, cache v6.1.0), `permissions: contents: read`, no secret, no `pull_request_target`, no untrusted input in scripts, `persist-credentials: false`, SDK from `global.json`, NuGet cache keyed on `Directory.Packages.props`, `.csproj` and `global.json`, no artefact upload.
- Cost, checked 2026-11-13 on https://docs.github.com/en/billing/concepts/product-billing/github-actions: standard runners in public repositories are free; larger runners are billed and not used. The repository is public. First run on PR #61 (https://github.com/AndreaAzzarello/kalei/actions/runs/43438906646): green, job 1 min 35 s (about 2 billable minutes, rounded up per job; nothing charged on a public repository). Longest steps: Set up .NET 23 s, App publish 15 s, Restore 11 s, format 6 s, Worker publish 5 s; tests 30 of 30 in 1 s; cache was empty on this first run. SDK 10.0.404, runtime 10.0.15, published runtime pack 10.0.15 (check passes). Image windows-2025 version 20261103.629. The account billing page (web login) is not read by the agent: the owner checks it.
- Whole sequence run locally first: 30 tests pass, format clean, both publishes without warnings, Worker exit 0, runtime 10.0.15. Merge protection is only a recommendation (`docs/tickets/README.md`).

## 2026-11-13 #39 — Graphify rule: code-only updates
- `CLAUDE.md`: the graph is updated only with `/graphify . --code-only --update` (AST, free). Document extraction only on the owner's explicit request.
- Reference cost: the full graph of 2026-11-13 used 547,410 tokens, all on documents; code cost 0.

## 2026-11-13 #38 — Ticket 05 merged (#60); no .pdb in releases; Graphify index
- PR #60 squash-merged (ticket 05, issue #6 closed). Tickets 30 (criterion 6, ZIP) and 31 (criterion 7, installer) now require that the release publish contains no `.pdb` file, in particular the native ones of SkiaSharp and HarfBuzzSharp (about 100 MB in a plain publish); checked by listing the files; issues #37 and #38 updated.
- Graphify run on the repository (129 files: 36 code, 82 documents, 11 images skipped to save tokens): 533 nodes, 788 edges, 35 communities. Code (AST) 212 nodes and 324 edges at no token cost; documents 316 nodes and 535 edges from 4 Haiku subagents, 547,410 tokens in total (input and output together). Health: 68 edges with an endpoint that is not a node, 25 collapsed same-endpoint edges. `graphify-out/` is local and ignored by git.

## 2026-11-13 #37 — Ticket 05: samples, publish checks, documents
- `tests/samples/README.md` (rules; large samples generated, `tests/samples/generated/` ignored by git). A guard test keeps `InvariantGlobalization` out of `Directory.Build.props`, off in the App and unset in Core and Donut.
- Publish checked locally with warnings as errors: App (ReadyToRun + trimmed, self-contained win-x64) and Worker (trimmed) both succeed with no trimming warning; the published Worker exits 0; App assemblies carry the ReadyToRun header, the Worker's do not (as decided). App folder 55.0 MB without `.pdb` files, but the publish also copies two native `.pdb` (libSkiaSharp 80 MB, libHarfBuzzSharp 20 MB) that the release pipeline must leave out (tickets 30 and 31).
- Measured in invariant mode (TRD): `it-IT` cannot be created and `1.234,56` fails with the invariant culture, while an explicit `NumberFormatInfo` in code parses and formats it. Not verified: cold start of the window (ticket 10a), the CI (ticket 39), other machines.

## 2026-11-13 #36 — Ticket 05: logging abstraction without paths (criterion 7, S10)
- `IKaleiLog.Write(LogEvent)`; `LogEvent` holds only a level, a code and an optional duration (no string, object, Uri, FileSystemInfo or Exception). A shape test reads every public member of the logging types and fails on any free-text or path-like type; eight deliberately bad sample types prove that each way of smuggling a path in is caught.
- The file writer with rotation and the log scan are ticket 26.

## 2026-11-13 #35 — Ticket 05: adapter interfaces and Windows paths (criteria 2 and 3)
- Core.Platform: `IPlatformPaths` and `IInstallerLayout` complete (`InstallMode`, `DataFolderStatus`); `ISandboxLauncher`, `IPlatformImageCodec` and `IFileOrigin` are empty and name the ticket that defines their members (07a/07b, 09a/18, 27). No hook or shell interface.
- Platform.Windows: `WindowsInstallerLayout` (marker `kalei.portable`) and `WindowsPlatformPaths` (installed `%LOCALAPPDATA%\Kalei`, portable `data` next to the executable; logs, settings.json, temp). `CheckDataFolder` reports a folder that cannot be written. Tests written first cover both modes and the not-writable case.

## 2026-11-13 #34 — Ticket 05: dependency test (criterion 4)
- Metadata scanner (System.Reflection.Metadata, nothing loaded) with one rule per assembly: Core (no System.Net.*, Process, Pipes, Microsoft.Win32.* except SafeHandles, native calls), App (no System.Net.*), Donut (no UI or OS API, no other Kalei project). A fixture that breaks every rule proves the scanner sees each one; the three real assemblies pass.
- Tests run on Microsoft.Testing.Platform (xunit.v3), which .NET 10 requires for `dotnet test` (set in `global.json`).

## 2026-11-13 #33 — Ticket 05: solution, projects and build settings
- Kalei.slnx with Kalei.App (Avalonia 12.1.6, empty window), Kalei.Core, Kalei.Donut, Kalei.Platform.Windows, Kalei.Worker.Builtin and tests/Kalei.Tests (xunit.v3); all net10.0, central package versions, `global.json` pins SDK 10.0.404 (runtime 10.0.15 installed, above the 10.0.14 floor).
- Build settings: warnings as errors, analyzers at latest-recommended, NuGet audit on, no solution-wide InvariantGlobalization (App explicitly off, Worker on with the TRD constraint). Build and format check pass.

## 2026-11-13 #32 — Ticket 05 preparation: decisions recorded before the skeleton
- Ticket 03d: the meaning of "smooth" is confirmed (at most 1% of frames over 33 ms, median within 10% of the refresh period).
- Spec and ticket 05 aligned: no interface for the global pointer hook or shell integration in 1.0; the Core ban on `Microsoft.Win32.*` excepts `Microsoft.Win32.SafeHandles` (file handles for the Broker, ticket 08).
- Globalization decided: `InvariantGlobalization` is not set solution-wide. App off (Italian formatting); Core and Donut never use the OS culture; the Worker may use the invariant mode only with explicit `NumberFormatInfo` in code, with a test of `1.234,56` on the published Worker (tickets 20a, 20b, 22; TRD with the Microsoft sources and what they do not state).

## 2026-11-12 #31 — Ticket 03d (issue #58): software rendering or GPU
- New ticket 03d (group:1.0, size S, needs-owner-hardware, issue #58): on the Windows 11 PC with a GPU compare the default (GPU) with software rendering on warm start-up, Donut latency at 100% and 300%, smoothness of the ring rotation and colour change (frame times over 5 s) and CPU use. Software rendering is adopted only if the Donut stays at 100 ms or less and the animation is smooth; the automatic choice (software only without a usable GPU) is evaluated with Avalonia's documentation (default list `AngleEgl`, then `Software`; automatic fallback when no GPU).
- The meaning of "smooth" (at most 1% of frames over 33 ms, median within 10% of the refresh period) is a proposal in the ticket, to be confirmed before the run.
- Ticket 10a gets a criterion: cold start of the real window within 1.3 s on the reference machine, with the font risk recorded. Tickets are now 49 (index, `CLAUDE.md` and the labels page updated); links and ticket structure checked (0 errors).

## 2026-11-12 #30 — W7 decision: threshold 1.3 s, ReadyToRun + trimming ships, AOT optional
- Owner decision after ticket 03c. **Threshold revised from 1 s to 1.3 s** (median of 10 cold starts, second frame, reference VM with 2 vCPU, 4 GB, SATA SSD, Defender). Reason: the cold start is the worst case, and the measured values go from about 970 ms (files already scanned) to about 1120 ms (first start after copy, #47), both inside with a headroom of 332 and 177 ms; the 1 s was an initial value not derived from data. Revised after the measures, with the figures recorded in ticket-0. No warm target existed in the documents (418 ms is a measurement).
- ADR 0001 now says the UI ships with ReadyToRun + trimming; Native AOT is **not mandatory**, an opportunistic trial after the CI (ticket 38) with numbers to beat on the VM (968 ms; 811 ms with software rendering) and the same kit without the automatic sign-in. PRD, TRD, architecture, 1.0 spec, release pipeline, tickets 03a, 03c and 38 follow.
- Software rendering is not adopted yet (ticket 03d, issue #58): GPU against software on the Windows 11 PC. Open risk: the Bricolage Grotesque font is not in the spike and may raise the start-up time; measured again with the real window (ticket 10a).

## 2026-11-12 #29 — W7 cold-start breakdown on the reference VM (ticket 03c, issue #56)
- 45 valid cold starts of six variants of the spike window (ReadyToRun + trimming, same measure as #47, reboot + 150 s before each start, unmeasured warm-up, order rotated; a, b, d x10, c, e, f x5). Baseline **968 ms** median (925–993), 10 of 10 under 1 s, where #47 gave 1122.7 ms: the method differs and the data cannot say which factor weighs; the baseline also rises about 7 ms per round.
- Where the time goes (baseline): first managed code 210 ms, up to Main 278 ms (29%), builder + Avalonia/Skia platform 375 ms (39%), theme + window + layout + second frame about 300 ms. About two thirds of the start is waiting (CPU 313 ms of 968), cause not measured.
- Variants: window first (b) 918 ms but complete content 986 ms, no real gain; lean start (c) +23 ms; runtime options with ReadyToRun composite (d) **+578 ms**, worse in every phase; b + c + d (f) +530 ms; software rendering (e) **811 ms, −150 ms** (platform services 280 → 153 ms).
- Not verified: Native AOT, software rendering on a PC with a GPU, the cause of the drift and of the gap to #47, the real font in the window. Raw data and report stay out of the repository; code and kit on `spike/03a-build` (`92efe87`, `8abb070`).

## 2026-11-11 #28 — Reference VM results (issue #47): W1–W4, W8, W10 pass, W7 fails
- Hyper-V VM, Windows 10 Pro 22H2 build 19045.6456, 2 vCPU, 4 GB, host disk a SATA SSD; firewall services and profiles on, Defender only, no third-party firewall. **W1–W4 pass** (15 pass, 0 fail): the sandbox converts PNG to AVIF and WebP, TCP, UDP, DNS and loopback fail inside and succeed outside, handles are seekable, no admin needed; **W8 pass** (trimmed Worker 10 of 10). ADR 0003 and 0006 are now accepted for Windows 11 and Windows 10 22H2; the stopped-firewall states stay open (W19, W20).
- **W7 fails** with ReadyToRun + trimming: median 1122.7 ms over 10 cold starts (min 1020.8, max 1614.8) against 1000 ms. ADR 0001 unchanged, decision open: new ticket 03c (issue #56) breaks the start into phases and tries optimisations without AOT before the owner chooses between AOT (ticket 38), a revised threshold and optimisation. Tickets are now 48.
- **W10 passes** (criterion 1 of ticket 04): 10 valid cold rows, frame2 median 32.2 ms, max 53.5 ms, 10 of 10 under 100 ms, drops Copy and source unchanged 10 of 10. One file per drop and one screen at 100%; the first round (31.4 ms by eye) was lost when the kit restarted the phase. Issue #5 closes.
- The kit's final script hung in the W10 phase: `Get-Content` lines in the JSON expand to a huge object tree in Windows PowerShell 5.1 (`PSPath`, `PSDrive`), so `w10.json` was never written and a restart repeated W10. Reproduced on the host; the sanitised report was composed there from the raw files. The firewall kit (W19) does not use that code.
- The temporary automatic sign-in was removed by the system task (reported `ok`); the report's own registry check did not run because the VM was already back at its checkpoint.

## 2026-11-10 #27 — Donut scaling proof (ticket 04, criterion 3 passes)
- Second run on the Windows 11 dev PC with the fixed log: 150% (10 valid cold rows, frame2 median 36.0 ms, max 43.5), 200% (5 rows, 28.2 ms), and the window across the edge of two screens at 150%/100% (28.0 ms) and 200%/100% (26.3 ms), 5 rows each. 25 of 25 under 100 ms, ring inside the window 25 of 25, drops Copy and source unchanged 25 of 25, ring whole and centred by eye. The 150% series is the slowest, well under the limit; cause not measured.
- The recorded screen index differs between series because the straddling window sits half on each screen; the ring is placed from the cursor in the window's own units, not from the screen. One pair (index 2, scale 2.00) is not explained.
- Criterion 3 of ticket 04 passes; issue #5 stays open for criterion 1 only (latency on the reference VM, #47). ADR 0008 unchanged.
- Ticket 10a (#13) gets a new criterion: initial and minimum window size fit 1366×768 at 100% and 1920×1080 at 200% with no clipped content (the 900×620 DIP spike window did not fit the latter).

## 2026-11-10 #26 — In-app Donut spike (ticket 04, W10)
- Windows 11 dev PC, three monitors (100%, 300%, 100%), warm: overlay on the second frame after the first drag-enter in 24.5–30.2 ms median (max 31.4), 15 of 15 valid cold rows under the 100 ms limit; ring inside the window in 15 of 15, also with the window across two 100% screens. Not 10 valid runs per series, and not the reference VM.
- Format known before the drop: file list and extension at drag-enter, header sniff of 16 bytes in 1–2 ms; unknown extension shows "analysing". Drop reported as Copy, source unchanged: 27 of 27.
- Missing rows were a log bug, not the first run: a 1 s timer wrote the row and the window closed first after a quick drop. Fixed on `spike/04-donut` (row at the second frame, `enter` column, runs repeated until valid); re-entries had also lowered the first medians.
- Open: VM (#47), 150% and 200%, window across screens with different scaling. ADR 0008 unchanged (no fallback); #5 stays open.

## 2026-11-10 #25 — Canary without Internet probes; CI ticket
- Owner decision: the network canary makes no outbound connection to the Internet. Removed the probes to 1.1.1.1 and example.com and the DNS probe. Two TCP probes remain: `local` (the Core's listener on 127.0.0.1) and `net` (192.0.2.1:443, TEST-NET-1, RFC 5737). The verdict follows the Winsock code: `WSAEACCES` is blocked, timeout or unreachable is `unproven`, a connection is `leaked`; at most 1.2 s. Sources: RFC 5737 §3–4, Windows Sockets error codes, WFP ALE layer pages.
- Not stated by Microsoft (so W20 measures it, also offline and with the adapter disabled): which layer gives which code, and whether the connect filter runs before route selection. Our reading that DNS is done by the DNS Client service is not stated in a page we read, so it is marked as such. The notice `NETWORK_ISOLATION_UNPROVEN` is neutral, at most once per session, and says that Conversions work normally.
- New ticket 39 (issue #52): CI on GitHub Actions (build, test, format, trimming warnings; actions pinned by SHA; `contents: read`; SDK 10.0.14 or later). Tickets 30 and 38 now depend on it. Tickets are now 47. Remote branches `docs/01-sandbox-results` and `docs/03a-build-results` deleted (owner authorisation).

## 2026-11-10 #24 — Build spike (ticket 03a, W7–W9)
- Native AOT could not be tried: no MSVC linker on the dev PC (owner decision: not installed). It will be tried on a GitHub Actions Windows runner (ticket 38, issue #51); ADR 0001 stays unchanged until then. The ILCompiler advisory (patched in 10.0.14) also covers the .NET runtime that app and Worker ship: release builds must use 10.0.14 or later. ReadyToRun + trimming works: window start-up median 418 ms warm, app 23.5 MB zipped (Windows 11 dev PC, to repeat cold on the VM, #47).
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
