---
status: proposed
---
# Workers run in a Job Object + AppContainer and receive handles, not paths

Every Conversion runs in a Worker inside a Windows Job Object (memory limit, timeout, kill on cancel and on app exit) and an AppContainer without the network capability, so "no network" is enforced by the OS and not by good behaviour. The Worker has no filesystem access: the Broker opens the Source file and Output file and passes only handles/streams. No temporary copies, so large files cost no extra time or disk.

Status is *proposed* until Ticket 0 proves that AppContainer + the .NET runtime + libvips (and WIC for HEIC) work together. If AppContainer is unavailable, the fallback is Job Object + restricted token, with the weaker guarantee documented in the UI and TRD.

## Considered options

- Temp-folder copy with path access: simpler but doubles I/O on big files and gives the Worker filesystem access.
- In-process conversion: no isolation from malformed or malicious files.

## Consequences

- Engines that need seekable random access to several files (e.g. some FFmpeg muxers) may need an explicit exception, decided when that Module is built.
