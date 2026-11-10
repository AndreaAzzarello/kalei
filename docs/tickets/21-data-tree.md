# 21 — Data: JSON, YAML and XML

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [07](07-launcher-and-worker-ipc.md), [19](19-text-like-detection.md)
- **Rules**: S2, S9
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
A user converts between JSON, YAML and XML, with hostile inputs failing safely.

## Acceptance criteria
1. All pairs among JSON, YAML and XML work, with XML using `@attribute` and `#text`.
2. YAML never instantiates arbitrary types and stops above 100 aliases or depth 64 with E_COMPLEX; JSON and XML stop above depth 64.
3. XML with a DTD or external entities is rejected.
4. A "billion laughs" sample and a deeply nested sample fail cleanly within the Limits.
5. The 200 MB file limit is enforced.
