# 31 — Per-user installer and clean uninstall

- **Status**: needs-owner-hardware
- **Size**: L
- **Blocked by**: [01](01-sandbox-spike.md), [03](03-build-and-size-spike.md), [30](30-portable-zip-sums-notices.md)
- **Rules**: S1, S5
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
A user installs Kalei without administrator rights, the sandbox works straight away, and uninstalling leaves nothing behind except the settings they chose to keep.

## Acceptance criteria
1. The installer installs per user without administrator rights and creates a Start menu entry.
2. It grants read and execute on the install folder to ALL APPLICATION PACKAGES; after install the sandbox starts and a conversion succeeds on a clean Windows 10 VM and on Windows 11.
3. AppContainer profiles are created at first run and removed at uninstall through the app command.
4. Uninstall removes program files, shortcuts, profiles and the temporary folder, and keeps settings unless the user ticks the option to delete them.
5. Upgrade over an existing install keeps settings.
6. The portable build shows a clear message and refuses to convert if the sandbox cannot be set up.
