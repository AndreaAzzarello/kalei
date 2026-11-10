---
status: accepted
---
# Kalei stays independent; Filee is an inspiration, not a base

Filee (https://github.com/KnifeLemon/Filee, MIT, Avalonia + .NET 10) pioneered converting files by dropping them on a radial menu at the cursor. After reading its documentation and code (read only, nothing copied) we keep Kalei independent instead of forking it or contributing to it, and we compete on a narrow, verifiable promise: file parsing in sandboxed Workers without network, no connection started by Kalei itself, and only permissive or LGPL engines.

Why not a fork: Filee runs most built-in engines inside the app process and moves daily; retrofitting a sandboxed-worker architecture would touch everything and the fork would drift from upstream faster than one person can merge. Why not contribute-and-stop: a sandboxed Worker model is not a pull request, and stopping would drop Kalei's only real advantage.

## Consequences

- No Filee code or assets are copied. If that ever changes it needs the owner's explicit approval, an entry in the third-party notices and the MIT text.
- The README credits Filee as inspiration and states only differences that can be checked.
- Scope stays smaller than Filee on purpose (see PRD, "Out of scope"): we do not chase its 270 formats.
