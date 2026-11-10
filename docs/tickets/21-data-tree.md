# 21 — Data: JSON, YAML and XML

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [07b](07b-windows-sandbox-launcher.md), [19](19-text-like-detection.md), [09a](09a-images-module-core.md)
- **Rules**: S2, S9
- **Stories**: 41 ([user stories](../spec/1.0-windows.md#user-stories))
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
A user converts between JSON, YAML and XML, with hostile inputs failing safely.

## Acceptance criteria
1. All pairs among JSON, YAML and XML work, with XML using @attribute and #text.
2. YAML never instantiates arbitrary types; the alias count comes from the parser events and the alias and depth limits of the Limits table give E_COMPLEX; JSON and XML stop at the depth limit with E_COMPLEX.
3. XML with a DTD or external entities is rejected with E_FORMAT.
4. A "billion laughs" sample and a deeply nested sample fail cleanly within the Limits.
5. The data and text size limit is enforced (E_SIZE).
6. The preselected targets (JSON → YAML, YAML → JSON, XML → JSON) match the spec table, and the card shows the first lines.
