# 03b — Licence and signing checks: Inno Setup, SignPath, logotype font

- **Status**: ready-for-agent
- **Size**: S
- **Blocked by**: –
- **Rules**: none
- **Stories**: none (constraint, spike or infrastructure)
- **Proofs**: W18 ([ticket-0](../ticket-0.md))
- **Spec**: [ticket-0, Part A](../ticket-0.md#part-a--windows-blocks-10)

## What to build
Read the licence and eligibility terms that gate the installer and signing, so the release pipeline can rely on them. No hardware needed.

## Acceptance criteria
1. W18: the Inno Setup licence text of the version to be used is read and summarised in the TRD.
2. W18: the SignPath Foundation eligibility terms are read and summarised; whether the project qualifies is recorded as yes, no, or "needs the owner to apply".
3. If either is unsuitable, an alternative is proposed to the owner and not adopted without their approval.
4. The original licence text of the logotype font (Mayenne Sans, see the TRD to-do) is obtained, or the question is handed to the owner with what is missing.
   - **Superseded (2026-11-10)**: owner decision — only Bricolage Grotesque (OFL 1.1) is cited and Mayenne Sans is not needed (see the [TRD](../TRD.md) and HISTORY #19). This criterion no longer applies; issue #4 stays closed.
