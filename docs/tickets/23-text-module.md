# 23 — Text: TXT, Markdown and HTML

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [07b](07b-windows-sandbox-launcher.md), [19](19-text-like-detection.md), [09a](09a-images-module-core.md)
- **Rules**: S9
- **Stories**: 45 ([user stories](../spec/1.0-windows.md#user-stories))
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
A user converts Markdown to a clean web page, HTML back to Markdown, and either to plain text, without the output being able to leak or run anything.

## Acceptance criteria
1. MD to HTML gives a full page with minimal style, sanitised, with the Content-Security-Policy of S9 and no remote resources or scripts.
2. HTML to MD, MD to TXT, HTML to TXT and TXT to TXT (encoding and line endings) work.
3. Hostile HTML (script, event handlers, remote images, javascript: links) is neutralised in tests.
4. The data and text size limit is enforced (E_SIZE).
5. The preselected targets (MD → HTML, HTML → MD, TXT → TXT) match the spec table, and the card shows the first lines.
