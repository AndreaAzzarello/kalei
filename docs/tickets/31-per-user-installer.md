# 31 — Per-user installer, clean uninstall, final size and antivirus checks

- **Status**: needs-owner-hardware
- **Size**: M
- **Blocked by**: [01](01-sandbox-spike.md), [03a](03a-build-spike.md), [03b](03b-licence-and-signing-checks.md), [07b](07b-windows-sandbox-launcher.md), [30](30-portable-zip-sums-notices.md)
- **Rules**: S1, S5
- **Stories**: 61, 65 ([user stories](../spec/1.0-windows.md#user-stories))
- **Proofs**: W8, W17 ([ticket-0](../ticket-0.md))
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
A user installs Kalei without administrator rights, the sandbox works straight away, and uninstalling leaves nothing behind except the settings they chose to keep.

## Acceptance criteria
1. The installer installs per user without administrator rights and creates a Start menu entry.
2. It grants read and execute on the install folder to ALL APPLICATION PACKAGES; after install the sandbox starts and a conversion succeeds on a clean Windows 10 VM and on Windows 11.
3. Uninstall removes the program files, the shortcut, the AppContainer profile (through the clean-up command), the journal and the temporary folder, and keeps settings and logs unless the user ticks the option to delete them; an upgrade over an existing install keeps settings.
4. W8, final: the installer and the ZIP are measured; 45 MB or less is the target and 60 MB the limit; above that the scope is reviewed with the owner.
5. W17: Microsoft Defender, one other free antivirus (named in the report) and SmartScreen results for the installer and the app are recorded.
6. The portable build refuses to convert with a clear message if the sandbox set-up fails (behaviour of ticket [07b](07b-windows-sandbox-launcher.md), checked here).
7. The installer ships no `.pdb` file, in particular not the native ones of SkiaSharp and HarfBuzzSharp. Checked by listing the files of the folder the installer is built from and of the folder after an install; the size measure of criterion 4 is taken without them.
