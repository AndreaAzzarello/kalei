# 32 — Code signing, only if SignPath is confirmed

- **Status**: ready-for-agent
- **Size**: S
- **Blocked by**: [31](31-per-user-installer.md)
- **Rules**: none
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
If ticket 03 confirmed that the project is eligible, the installer and executables are signed; otherwise this ticket is closed and the README says 1.0 is unsigned.

## Acceptance criteria
1. If W18 is positive, installer and executables are signed through the pipeline, and the signature verifies on a clean machine.
2. SmartScreen behaviour with the signed installer is recorded.
3. Credentials live only in repository secrets, never in the repository.
4. If W18 is negative, the ticket is closed as not applicable and the README and release notes say 1.0 is unsigned and how to verify the sums.
