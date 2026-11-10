# 10 — Main window: convert one file from drop to Done

- **Status**: ready-for-agent
- **Size**: L
- **Blocked by**: [09](09-images-module.md)
- **Rules**: S3, S10
- **Spec**: [1.0 Windows](../spec/1.0-windows.md)

## What to build
A user drops one file, sees what it is, checks the proposed target, converts, sees progress and the result, in a window that takes the colour of the file.

## Acceptance criteria
1. States 1, 2, 4, 5, 6, 7 and 8 of the app flow work for a single file, including Choose files, reachable by keyboard.
2. The file card shows the real Format and size, and the extension-mismatch notice.
3. The target list offers the valid Formats with the preselected default from the spec.
4. Convert shows progress; Cancel kills the Worker and removes the partial Output; Done shows the output name, the real size, Show in folder and Convert another.
5. Errors use plain language with a short code (English at this stage; ticket 25 translates).
6. The accent follows the Category; light and dark follow Windows; "Colour transition" off or Windows "Reduce animations" makes changes instant.
7. The window makes no network call and has no update check (S6); it never decodes untrusted content itself (S3).
