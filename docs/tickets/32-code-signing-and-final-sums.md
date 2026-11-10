# 32 — Code signing, only if confirmed, and the final SHA256SUMS

- **Status**: needs-owner-hardware
- **Size**: S
- **Blocked by**: [31](31-per-user-installer.md), [03b](03b-licence-and-signing-checks.md)
- **Rules**: none
- **Stories**: none (constraint, spike or infrastructure)
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
If ticket [03b](03b-licence-and-signing-checks.md) confirmed that the project is eligible, the executables and the installer are signed; either way the final checksum file is produced last, over the final files.

## What ticket 03b found (2026-11-10, [TRD](../TRD.md))
The owner will apply to the SignPath Foundation **after the first public release**; 1.0 may ship unsigned. The terms ([signpath.org/terms](https://signpath.org/terms)) require:
- a public release already out in the form to be signed (so the application cannot come before it);
- multi-factor authentication for every team member, on SignPath and on GitHub;
- manual approval of every release for signing, and binaries built from source in a verifiable way;
- a "Code signing policy" page on the project site with the line "Free code signing provided by SignPath.io, certificate by SignPath Foundation", the team roles and a privacy statement.
The certificate is issued to SignPath Foundation, which may refuse or revoke. Not verified: the `/apply` form and the supported CI.

## Acceptance criteria
1. If W18 is positive, the executables and the installer are signed through the pipeline and the signature verifies on a clean machine; SmartScreen behaviour with the signed installer is recorded.
2. Credentials live only in repository secrets, never in the repository.
3. If W18 is negative, signing is closed as not applicable and the README and release notes say 1.0 is unsigned and how to verify the sums.
4. Either way the final SHA256SUMS is computed as the last step over the final installer and ZIP and verified with Get-FileHash.
