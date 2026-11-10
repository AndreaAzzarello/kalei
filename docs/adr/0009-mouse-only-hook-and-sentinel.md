---
status: proposed
---
# Global Donut: mouse-only hook, modifier state polled, and a minimal Sentinel process

The global Donut (1.2) uses a low-level **mouse** hook (`WH_MOUSE_LL`) written by us and reads the chosen modifier key with `GetAsyncKeyState` only while a drag is in progress. There is **no keyboard hook**; Kalei never records keystrokes. The mouse wheel flips the Donut's Face.

The hook lives in a **Sentinel**: a tiny, separate process (target at most 20 MB of RAM at rest, to be measured) that only watches the mouse and starts the overlay/UI when the gesture (modifier held + button down + movement past a threshold) occurs. The full Avalonia app is not kept in memory. Residency and start at sign-in are opt-in and off by default. The Sentinel makes no network connection and writes no input to disk or log.

README wording (accurate, replaces "never reads the keyboard"): *"Kalei never records keystrokes. During a drag it only checks whether your chosen modifier key is held."*

SharpHook (MIT) wraps libuiohook, whose repository contains both GPL-3.0 and LGPL-3.0 texts without a statement of which applies (checked 2026-11-10); that ambiguity is one more reason to write the small hook ourselves.

Status is *proposed* until Ticket 0 measures: gesture-to-overlay latency from a cold start (the hook sees the gesture, the files arrive only at the first `DragEnter` on the overlay), Sentinel RAM, whether wheel events reach the hook during an OLE drag, and antivirus behaviour. If cold start is too slow, a pre-started lightweight overlay is considered then.

## Consequences

- 1.2 requires a signed build (SignPath Foundation or equivalent, eligibility not yet verified).
- The in-app Donut (1.0) is unaffected: no hook, no Sentinel.
