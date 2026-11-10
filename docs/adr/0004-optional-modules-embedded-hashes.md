---
status: accepted
---
# Optional modules: fixed source, SHA-256 embedded in the app, explicit consent

**Applies from 1.1**, together with the first Optional module. Release 1.0 has only built-in Modules and makes no network connection at all. Integrity rules for installed modules are in [architecture.md](../architecture.md) (S8).

An Optional module is downloaded only after an explicit consent dialog showing its size, only from the Releases of `AndreaAzzarello/kalei`, and only if its SHA-256 matches the value compiled into the app. It installs to `%LOCALAPPDATA%\Kalei\modules`, can also be installed offline from a local `.kmod` (same hash check), can be uninstalled, and then works offline. No background or hidden download ever happens.

**Accepted trade-off:** because hashes live in the app binary (not in a downloaded manifest an attacker could swap), every Module update requires a new app release.

This is the only network connection Kalei is allowed to make. There is no auto-update and no update check: "Check for updates" is a manual button that opens the Releases page in the browser. Because the engines of installed modules receive no automatic patches, each app release publishes SHA-256 sums of the installers, and the repository carries a `SECURITY.md` with the advisory policy (created at repository setup).
