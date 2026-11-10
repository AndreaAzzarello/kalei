---
status: accepted
---
# A module is a process, not a .NET plugin

Each Module is an executable Worker with its native Engines and a `module.json` manifest (input/output formats, options, limits), talking to the Core over a versioned IPC protocol on pipes. We do not load module DLLs into another process: trimming makes dynamic loading fragile, and a process boundary is what lets us sandbox untrusted file parsing. A Module may be written in any language.

The MVP Modules (images, data, text) share one built-in worker, `Kalei.Worker.Core`.

## Consequences

- Each Optional module duplicates the .NET runtime (~15–25 MB), negligible next to engines like FFmpeg.
- The IPC protocol is a versioned public contract between app and modules.
