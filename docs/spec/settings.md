# Kalei 1.0 — Settings, localisation and logs

Part of the [1.0 spec](1.0-windows.md). Terms: [GLOSSARY](../../GLOSSARY.md). Locations come from the paths adapter, never from code that knows the OS (see the [capability table](../architecture.md#capability-table-per-os)).

## Where data lives
- **Installed**: the per-user data folder of the paths adapter (on Windows `%LOCALAPPDATA%\Kalei`), holding `settings.json`, the `logs` folder, the journal and the temporary folder (S7).
- **Portable ZIP**: if a marker file named `kalei.portable` sits next to the executable, the same items live in a `data` folder next to the executable instead. Nothing else is written, except what the sandbox set-up needs (the AppContainer profile and the folder access, set at first run; the app has a clean-up command that removes them, see the [release pipeline](release-pipeline.md#sandbox-set-up-at-install-time)). Kalei needs a writable data folder for the journal and the temporary folder (S7): if it is not writable, Kalei says so and does not convert.

## `settings.json`
One JSON file, written atomically (write to a temporary name in the same folder, flush, rename over the old file). Contents:

| Key | Values | Default |
|---|---|---|
| `version` | integer, starts at 1 | – |
| `language` | `system`, `en`, `it` | `system` |
| `theme` | `system`, `light`, `dark` | `system` |
| `colorTransition` | true / false | true |
| `outputFolder` | `mode`: `nextToSource` or `chosen`; `path` when chosen | `nextToSource` |
| `nameClash` | `rename`, `ask` ("Apply to all" in the dialog covers the current Batch only and does not change this setting) | `rename` |
| `limits` | only the values the user changed (see Limits below) | none |

Rules:
- **Per-Conversion Options** (quality, size, metadata, CSV choices) are not saved between runs in 1.0: the Advanced panel starts from the defaults of the spec and remembers the last values only while the app runs.
- **Unknown keys are kept** when the file is rewritten, so a newer version's settings survive a downgrade.
- **Migration**: when `version` is lower than the app's, the app migrates step by step and rewrites the file; a higher version is read as far as understood and never lowered.
- **Corrupt or unreadable file**: Kalei starts with the defaults, copies the bad file to `settings.json.bad` once, and shows a short notice.
- The file holds a folder path (the chosen output folder) because the user asked for it; it is local only and is never logged (S10).

### Limits
Settings → Limits shows the editable values of the [Limits table](../architecture.md#limits) with their defaults and the **hard maximum** of the table, which cannot be exceeded (a larger value is rejected with a message). The editable ones are: file size for images and for data and text, pixels, files per Batch, folder depth, Worker memory and the timeout cap. Not editable in 1.0: the parallel Workers formula, the bomb limits (expansion ratio, total size, nested archives), YAML aliases and depth, and the JSON and XML nesting depth. A "Reset to defaults" button restores every value. Per-Conversion Options remain in the Advanced panel of a Conversion.

## Localisation
- English is the source language; Italian must be complete before release (string freeze when the feature set is complete).
- Keys are `area.item` (for example `error.corrupt`, `state.empty.title`). Text never appears in code, in the manifest or in the logs; a manifest carries keys only ([module-manifest](../module-manifest.md)).
- The mechanism (picked in ticket 10a) must work with Native AOT or ReadyToRun and trimming (no runtime reflection over resource names) and is checked with a trimmed publish. Every ticket that adds text delivers its keys in English and Italian; ticket 25 adds the completeness test, the catalogue and the language-dependent formatting.
- The system language is used when `language` is `system`; if it is neither English nor Italian, English.
- Number, date and size formatting follow the chosen language.
- **Error catalogue**: every error code and every notice of [ipc-spec](../ipc-spec.md#error-codes) maps to one message key, with what happened and what to do. A test fails if a code has no message in either language, or if a message contains a path or file-name placeholder.

## Logs
- **Format**: one JSON object per line: timestamp (UTC), level, event code, duration in milliseconds where relevant, Format identifiers, Module id and version, error code. **Never** a path, a file name, a folder name, file content or a URL (S10).
- **Levels**: error and warning only; there is no diagnostic mode in 1.0.
- **Rotation**: at most 5 files of 1 MB; the oldest is deleted when a new one starts.
- **Open log folder** in Settings opens the folder in the file manager.
- **Test**: a test feeds conversions, successful and failing, with distinctive sample paths and names through the app and scans every log line for them and for any path-like text; it fails if one appears.
- Zero telemetry: nothing is sent anywhere (S5, S6).

## Out of Scope
Saved per-Conversion defaults, a diagnostic log level, cloud sync of settings, per-Module settings and installed-module management (1.1).

## Testing and further notes
Tested through tickets 24 (persistence), 25 (languages and catalogue), 26 (logs) and 28 (Limits page), using the seams of the [1.0 spec](1.0-windows.md#testing-decisions). The hard maxima are initial values (see [Limits](../architecture.md#limits)).
