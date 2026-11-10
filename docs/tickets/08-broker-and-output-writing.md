# 08 — Broker and Output file writing: safe temporary file, journal, naming

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [05](05-solution-skeleton.md)
- **Rules**: S4, S7
- **Stories**: 11, 36, 41 ([user stories](../spec/1.0-windows.md#user-stories))
- **Proofs**: W16 (journal) ([ticket-0](../ticket-0.md))
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
Output files are created safely: never over an existing file, never partially visible, never leaving debris, and the Worker never sees a path.

## Acceptance criteria
1. The Broker opens the Source file read-only and creates the Output file exclusively under a reserved temporary name in the Output folder, and passes only handles.
2. The final name is taken by a rename that never replaces; on a clash the name becomes name (1).ext, then (2) and so on; two concurrent Conversions can never take the same name.
3. A journal in Kalei's data folder lists the in-flight temporary files; after a simulated crash the next start deletes exactly those files and no others (W16, journal part).
4. Cancel, error and timeout remove the partial Output file.
5. Names are sanitised: traversal, reserved names, trailing dots and spaces, over-long paths; the extension comes from the target Format.
6. The Source file is byte-identical before and after (checked by hash in tests).
7. A read-only destination, a full disk and a destination that cannot hold the file give E_IO with a clear message and no partial Output file.
