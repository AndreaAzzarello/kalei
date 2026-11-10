---
status: accepted
---
# A module is a process, not a .NET plugin

Each Module is a manifest (`module.json`: formats, Options, version; **never Limits**, which come from the table in the app, see S8) plus its native Engines, hosted by a Worker executable and talking to the Core over a versioned IPC protocol on inherited pipes. We do not load module DLLs into another process: trimming makes dynamic loading fragile, and a process boundary is what lets us sandbox untrusted file parsing. Modules are first-party only: there is no third-party module path, and every module's hashes are compiled into the app.

The 1.0 Modules (images, data, text) are three Modules hosted by one Worker executable, `Kalei.Worker.Builtin`.

## Consequences

- Each Optional module duplicates the .NET runtime (~15–25 MB), negligible next to engines like FFmpeg.
- The IPC protocol is a versioned contract internal to Kalei, specified in [ipc-spec.md](../ipc-spec.md); it is not a public plugin API.
