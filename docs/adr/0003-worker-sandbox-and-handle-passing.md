---
status: proposed
---
# Workers run in an OS sandbox without network and receive handles, not paths

Every Conversion runs in a Worker inside an OS sandbox that blocks the network and bounds memory and time (kill on cancel and on app exit), so "no network" is enforced by the OS and not by good behaviour. On Windows (1.0): a Job Object plus an AppContainer without the network capability; the mechanisms for other systems are in the capability table of [architecture.md](../architecture.md) and in [ADR 0010](0010-multiplatform-architecture-windows-first.md). The Worker has no filesystem access: the Broker opens the Source file and Output file and passes only handles/streams. No temporary copies, so large files cost no extra time or disk.

Status is *proposed* until Ticket 0 proves it for the OS ([ticket-0.md](../ticket-0.md)). **No-network is a release gate (S5).** If AppContainer cannot be made to work with the .NET runtime, libvips and WIC, the promise "no network leaves a Worker" is removed from PRD and README for that OS; no fallback is sold as equivalent. A Job Object plus restricted token may still be used as defence in depth (it limits memory, time and what the process may touch, but does not block the network) and is described exactly so. Workers receive inherited anonymous pipes and handles, not named pipes.

## Considered options

- Temp-folder copy with path access: simpler but doubles I/O on big files and gives the Worker filesystem access.
- In-process conversion: no isolation from malformed or malicious files.

## Consequences

- Engines that need seekable random access to several files (e.g. some FFmpeg muxers) may need an explicit exception, decided when that Module is built.
