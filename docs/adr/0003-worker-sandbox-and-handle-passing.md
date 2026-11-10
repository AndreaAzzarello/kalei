---
status: accepted (proved on the owner's Windows 11 PC, ticket 01; the Windows 10 22H2 repeat is tracked in issue #47)
---
# Workers run in an OS sandbox without network and receive handles, not paths

Every Conversion runs in a Worker inside an OS sandbox that blocks the network and bounds memory and time (kill on cancel and on app exit), so "no network" is enforced by the OS and not by good behaviour. On Windows (1.0): a Job Object plus an AppContainer without the network capability; the mechanisms for other systems are in the capability table of [architecture.md](../architecture.md) and in [ADR 0010](0010-multiplatform-architecture-windows-first.md). The Worker has no filesystem access: the Broker opens the Source file and Output file and passes only handles/streams. No temporary copies, so large files cost no extra time or disk.

The Windows part was proved by the sandbox spike (results below and in [ticket-0.md](../ticket-0.md#w1-w2-w3-and-w4--sandbox-spike-ticket-01-run-2026-11-10)); **the promise is not yet proved on Windows 10 22H2** (the reference VM, issue #47), and until then the README must not claim it for Windows 10. **No-network is a release gate (S5).** If AppContainer cannot be made to work with the .NET runtime, libvips and WIC, the promise "no network leaves a Worker" is removed from PRD and README for that OS; no fallback is sold as equivalent. A Job Object plus restricted token may still be used as defence in depth (it limits memory, time and what the process may touch, but does not block the network) and is described exactly so. Workers receive inherited anonymous pipes and handles, not named pipes.

## Result of the sandbox spike (ticket 01, W1–W4, 2026-11-10)

Windows 11 dev PC (build 10.0.26300), .NET 10.0.13, libvips 8.18.7; code on the branch `spike/01-sandbox`, not merged. Every "must fail" proof had a positive control that succeeded outside the sandbox.
- **Mechanism that worked**: an AppContainer profile with **no capabilities** (not even the internet client), the Worker started with `PROC_THREAD_ATTRIBUTE_SECURITY_CAPABILITIES` and `PROC_THREAD_ATTRIBUTE_HANDLE_LIST` (only the pipe, the Source handle opened read-only and the Output handle opened `CREATE_NEW`), created suspended, put into a Job Object, then resumed. No administrator rights.
- **Network (S5)**: TCP connect to 1.1.1.1:443 failed (socket access denied), a raw UDP DNS query to 1.1.1.1:53 got no answer, `Dns.GetHostAddresses` failed (host unknown), TCP to a listener on 127.0.0.1 timed out. The LPAC variant is not needed: AppContainer alone blocks everything above, and in LPAC even socket initialisation fails.
- **Filesystem**: reading a canary file and the Source by path, listing the user profile, and writing to the temp folder, the install folder and the profile were all denied (0 of 6 allowed; 6 of 6 outside the sandbox).
- **Handles**: PNG to AVIF and to WebP from inherited handles, output decoded and checked in pixels by the launcher; a 4096×4096 tiled deflate pyramid TIFF (6 levels) and an AVIF were read through inherited handles, which are seekable (887 and 226 seeks).
- **Job Object**: 2 GB cap stopped an allocating Worker (out of memory at 1,472 MB, job peak 1,503 MB); a hung Worker was killed at the timeout; a child process could not be started (limit of one active process); the Worker disappeared within 3 ms of the app exiting normally and of the app being killed (kill-on-close).
- **Install folder (W4)**: before the grant the Worker did not start (host exit code `0x80008085`); after `icacls … /grant *S-1-15-2-1:(OI)(CI)RX` on a folder the user owns, the Worker, the .NET runtime and libvips loaded. Nothing else was granted.
- **Not verified**: Windows 10 22H2 (issue #47); behaviour with the Windows Firewall services stopped or a third-party firewall (here BFE and the firewall service run and all profiles are on); HEIC through WIC inside the sandbox (still ticket 18); cold-start timings (the run measured warm: 46 ms to the first message, about 80 ms to convert, 119 MB peak, median of 10).
- **For ticket 09a**: libvips sniffs the format from the stream's current position, so a stream reused for a second image must be rewound first.

## Considered options

- Temp-folder copy with path access: simpler but doubles I/O on big files and gives the Worker filesystem access.
- In-process conversion: no isolation from malformed or malicious files.

## Consequences

- Engines that need seekable random access to several files (e.g. some FFmpeg muxers) may need an explicit exception, decided when that Module is built.
