---
status: accepted
---
# Multiplatform architecture from the start; Windows released first

Kalei's architecture is multiplatform from day one: **the Core contains no operating-system API.** Everything that depends on the OS sits behind a **Platform adapter**, an interface with one implementation per OS chosen at start-up:

- `ISandboxLauncher`: starts a Worker in the OS sandbox with memory/time limits and handles (S1, S5).
- `IPlatformImageCodec`: system image decoders (WIC on Windows, ImageIO on macOS, libheif where present on Linux).
- `IGlobalPointerHook`: the global gesture of the 1.2 Donut (S11).
- `IPlatformPaths`: data, log, temporary and settings folders.
- `IShellIntegration`: file-manager context menu (1.1).
- `IFileOrigin`: origin mark of a file (Mark of the Web, quarantine), S12.
- `IInstallerLayout`: where an installer or package puts files, and portable mode.

The per-OS mechanisms are listed once, in the capability table of [architecture.md](../architecture.md#capability-table-per-os); the TRD links to it.

**Release order.** 1.0 is Windows only (10 22H2 and 11, x64). Linux and macOS come in 1.x, each only after its own spike ([ticket-0.md](../ticket-0.md), not blocking 1.0) and with the same release gate S5: if an OS's sandbox cannot be shown to block the network of a Worker, **the promise "no network" is not made for that OS**. The In-app Donut works on every OS. The Global Donut only where the OS allows it: Windows; macOS with Input Monitoring or Accessibility permission; Linux on X11 only, not on Wayland.

## Sandbox mechanism proposed per OS (to be proved in the spikes)
- **Windows**: Job Object plus AppContainer without the network capability ([ADR 0003](0003-worker-sandbox-and-handle-passing.md)).
- **Linux, base layer (no privileges, nothing to install)**: `no_new_privs`, then a seccomp-bpf filter that makes `socket()` fail for every family except `AF_UNIX` (this blocks TCP, UDP and DNS), then Landlock with no allowed paths (needs kernel 5.13; Landlock's own network rules need TCP ABI v4 and UDP ABI v10, so seccomp is what blocks the network on older kernels). Inputs arrive only as inherited file descriptors. Sources: [seccomp(2)](https://man7.org/linux/man-pages/man2/seccomp.2.html) ("no_new_privs" or CAP_SYS_ADMIN is required; a filter sees up to six syscall arguments), [kernel seccomp filter docs](https://docs.kernel.org/userspace-api/seccomp_filter.html), [kernel Landlock docs](https://docs.kernel.org/userspace-api/landlock.html) ("first introduced in Linux 5.13"; "setting the no_new_privs attribute is required"; "TCP bind and connect" restriction "starting with ABI version 4"). Not verified: which kernel release first ships Landlock's UDP rules; blocking of `io_uring` and 32-bit `socketcall` is our own recommendation, not from the docs.
- **Linux, additional layer**: bubblewrap with `--unshare-all --die-with-parent` only where unprivileged user namespaces work (a system `bwrap`, or inside Flatpak: `flatpak-spawn --sandbox --no-network --forward-fd`). Limits: bubblewrap needs user namespaces (its setuid mode was removed; [README](https://github.com/containers/bubblewrap), [man page](https://raw.githubusercontent.com/containers/bubblewrap/main/bwrap.xml)); Ubuntu 24.04 lets unconfined apps create user namespaces but "deny the subsequent use of any capabilities within the user namespace" ([release notes](https://documentation.ubuntu.com/release-notes/24.04/)), so a bwrap carried inside an AppImage will probably not work there (our inference, to prove in the spike). Flatpak gives "no access to the network" by default ([docs](https://docs.flatpak.org/en/latest/sandbox-permissions.html)); `flatpak-spawn` has `--sandbox`, `--no-network` and `--forward-fd` ([source](https://github.com/flatpak/flatpak-xdg-utils/blob/main/src/flatpak-spawn.c)). Fedora's user-namespace default was not verified.
- **macOS**: App Sandbox on the Worker: entitlements `com.apple.security.app-sandbox` and `com.apple.security.inherit` only, no `network.client`, started with `posix_spawn`, files passed as inherited descriptors. **It works with ad-hoc signing**: Apple's DTS engineer states the App Sandbox entitlement is not a restricted entitlement ([forums 694784](https://developer.apple.com/forums/thread/694784)), and Apple's own helper-tool article signs the helper ad hoc ([Embedding a helper tool in a sandboxed app](https://developer.apple.com/documentation/xcode/embedding-a-helper-tool-in-a-sandboxed-app); the tool must carry just those two entitlements). A child "always inherits its sandbox from the parent" ([forums 706390](https://developer.apple.com/forums/thread/706390)). Not verified (spike): that outbound connections are really blocked without `network.client` (the documentation only says the entitlement allows "outgoing network connections"; for TCP it restricts "only the initiation of a connection", so the parent must never pass socket descriptors to a Worker), and that a sandboxed child can use a descriptor it inherited (no Apple text found). `sandbox-exec` and `sandbox_init()` are deprecated ([man page](https://keith.github.io/xcode-man-pages/sandbox-exec.1.html)) and custom profiles are unsupported ([forums 747880](https://developer.apple.com/forums/thread/747880): "We don't support custom sandboxes at all"): rejected.

## Plan B for macOS without an Apple Developer account
Kalei must not depend on an Apple account. Worker security (no network, access only to handles) does not rely on it: it comes from App Sandbox, which works with an ad-hoc signature.
- Build: ad-hoc signed (`codesign -s -`). On Apple Silicon "any executable must be signed before it's allowed to run… a simple ad-hoc signature is sufficient" ([Big Sur 11.0.1 release notes](https://developer.apple.com/documentation/macos-release-notes/macos-big-sur-11_0_1-universal-apps-release-notes)).
- User step: since macOS Sequoia the Control-click override is gone; the user opens System Settings → Privacy & Security and presses **Open Anyway** ([Apple news](https://developer.apple.com/news/?id=saqachfa), [Apple support 102445](https://support.apple.com/en-us/102445)). The README explains it.
- What stays guaranteed: Worker sandbox and no network (same mechanism), no telemetry, hash-checked releases (SHA-256 sums published).
- What changes for the user: a Gatekeeper warning on first launch; an ad-hoc identity is not stable across builds (reported by Apple's DTS in [forums 722707](https://developer.apple.com/forums/thread/722707), not opened directly), so privacy permissions granted to the app (Input Monitoring for the Global Donut) may have to be granted again after each update.
- With an account: Developer ID signing and notarisation are a *distribution* layer only (`notarytool submit … --wait`, `stapler staple`; [Apple](https://developer.apple.com/documentation/security/customizing-the-notarization-workflow)). Apple states that if the membership expires, users "can still download, install, and run" applications signed with Developer ID, unless the certificate is revoked ([Developer ID](https://developer.apple.com/support/developer-id/)). No Apple text was found linking this to the secure timestamp or to notarisation tickets: **not verified**, so the project does not rely on it.

## Considered options
- Multiplatform only after 1.0: forces a rewrite of the Core once Windows APIs have spread through it.
- Three separate apps: triples maintenance for one person.
- A single sandbox library for all OSes: none exists that covers AppContainer, seccomp/Landlock and App Sandbox with handle passing.

## Consequences
- Every OS-specific item needs an adapter and an entry in the capability table; a Windows API in the Core is a defect.
- Linux and macOS ship only with their spike passed; an OS that fails S5 can still ship without the "no network" promise (the README says so), but not with a weaker mechanism sold as equivalent.
