# Kalei — Module manifest (draft v0)

Every Module, built-in or optional, is described by a `module.json`; the Core builds the [Conversion matrix](conversion-matrix.md) from the manifests at run time. Built-in Modules go through exactly the same loader as Optional ones (from 1.1).

## What a manifest may and may not say
- It declares: identity, version, protocol, Formats it reads and writes, the pairs it supports, and the **Options** each pair offers.
- It **never** declares limits (size, pixels, memory, time): the Core takes them from its own table ([Limits](architecture.md#limits)). A manifest with a `limits` field, or any unknown field, is rejected (S8).
- Hashes are not in the manifest: the hash of every file of an Optional module is compiled into the app, keyed by module id and version, and checked at install and at every load.

## Shape
\`\`\`json
{
  "id": "images",
  "version": "1.0.0",
  "protocol": 1,
  "worker": "Kalei.Worker.Builtin",
  "formats": {
    "png":  { "category": "image", "read": true, "write": true },
    "heic": { "category": "image", "read": true, "write": false }
  },
  "pairs": [
    { "from": "png", "to": "webp", "options": ["quality", "maxSize", "stripMetadata"] }
  ],
  "options": {
    "quality":       { "type": "int", "min": 1, "max": 100, "default": 82, "control": "slider", "label": "option.quality" },
    "maxSize":       { "type": "int", "min": 16, "max": 16384, "default": null, "control": "number", "label": "option.maxSize" },
    "stripMetadata": { "type": "bool", "default": false, "control": "toggle", "label": "option.stripMetadata" }
  }
}
\`\`\`

## Options type system
`type` is one of `bool`, `int` (with `min`, `max`), `enum` (with `values`, each with a `label` key), `string` (with `pattern` and `maxLength`). `control` is a hint for the Advanced panel (`toggle`, `slider`, `number`, `dropdown`, `text`). `label` and enum labels are resource keys, never display text (English and Italian live in the app's string tables). `default: null` means "not set". The Advanced panel shows only the options listed for the selected pair and a "Reset to defaults". The Core validates every value against the type before sending `Convert`; the Worker validates again.

## `.kmod` layout (from 1.1)
A ZIP-like package with `module.json` at the root and the files under `files/`. Nothing else is read. Extraction rules and the whole-package hash check are S8; the file list in the manifest must match the content exactly.
