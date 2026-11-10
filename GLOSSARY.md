# Kalei

Kalei is a desktop app that converts files locally and privately. This glossary fixes the words used in code, docs and UI.

## Language

### Conversion

**Conversion**:
The transformation of one **Source file** into one **Output file** of a different **Format**.
_Avoid_: Export, transform, job

**Source file**:
The file the user wants to convert. Kalei never modifies, moves or deletes it.
_Avoid_: Input file, original

**Output file**:
The file produced by a **Conversion**, written next to the **Source file** or in the chosen **Output folder**.
_Avoid_: Result, target file

**Output folder**:
The place where **Output files** are written; by default the folder of each **Source file**.
_Avoid_: Destination, export dir

**Format**:
A concrete file type such as PNG or CSV, recognised from file content and not from the extension.
_Avoid_: Extension, type

**Category**:
A group of **Formats** shown with one accent colour and icon: image, audio, video, document, spreadsheet, archive, other.
_Avoid_: Family (that is a **Module** concept), kind

**Batch**:
A set of **Source files** (including those found in dropped folders) converted in one go, with one target **Format** per **Category**.
_Avoid_: Bulk, queue (the queue is the internal ordering of a **Batch**)

**Conversion matrix**:
The documented table of every supported *from Format → to Format* pair and the **Module** that handles it.
_Avoid_: Support table

**Options**:
The per-**Conversion** settings (quality, resolution, bitrate, sheets…) shown in the "Advanced" panel.
_Avoid_: Settings (those are app-wide)

**Limits**:
The maximum file size, pixel count, file count, folder depth, memory and time a **Conversion** may use.
_Avoid_: Quotas

### Architecture

**Core**:
The light part of Kalei that detects **Formats**, owns the queue, enforces security and manages **Modules**. It never converts anything itself.
_Avoid_: Engine, backend

**Module**:
A separately packaged converter for one family of **Formats**, run as its own process and described by a manifest.
_Avoid_: Plugin, extension, add-on

**Built-in module**:
A **Module** shipped inside the installer (in the MVP: images, data, text).
_Avoid_: Core module

**Optional module**:
A **Module** installed only after the user consents, from a fixed source with a verified hash.
_Avoid_: Downloadable, heavy module

**Engine**:
A third-party library or binary (libvips, FFmpeg, pandoc…) that a **Module** uses to do the actual work.
_Avoid_: Backend, codec

**Worker**:
The separate, sandboxed process in which a **Module** runs one **Conversion**.
_Avoid_: Child process, converter process

**Broker**:
The part of the **Core** that opens **Source file** and **Output file** handles and passes them to a **Worker**.
_Avoid_: Proxy, host

**Sandbox**:
The set of OS restrictions on a **Worker**: no network, no filesystem access, bounded memory and time.
_Avoid_: Jail, container

**Module package**:
A `.kmod` file containing an **Optional module**, installable online or offline.
_Avoid_: Bundle, archive

### Donut

**Donut**:
The ring of **Slices** that opens at the cursor while files are dragged; dropping on a **Slice** starts the **Conversion** without opening a window.
_Avoid_: Radial menu, wheel, pie

**Slice**:
One segment of the **Donut**, standing for one target **Format** (or, disabled, for a **Format** whose **Module** is missing).
_Avoid_: Button, option

**Face**:
One of the two sides of a **Donut**: the front with common **Formats**, the back with less common ones.
_Avoid_: Page, mode

**In-app Donut**:
The **Donut** shown inside Kalei's own window when files are dragged over it.
_Avoid_: Local donut

**Global Donut**:
The **Donut** shown at the cursor anywhere on screen, opened by the **Sentinel**; opt-in.
_Avoid_: System donut

**Sentinel**:
The tiny separate process that watches the mouse for the global gesture and starts the overlay on request.
_Avoid_: Tray app, daemon, service

### Experience

**Drop zone**:
The central area of the single window where the user drops files or folders.
_Avoid_: Dropbox, upload area

**Accent**:
The colour and icon of the interface, taken from the **Category** of the dropped file.
_Avoid_: Theme (light/dark is the theme)

**Estimate**:
The approximate output size shown before converting, computed by converting a small sample.
_Avoid_: Prediction

## Relationships

- A **Batch** contains one or more **Conversions**.
- Each **Conversion** runs in exactly one **Worker**, which belongs to exactly one **Module**.
- A **Module** wraps one or more **Engines** and declares which **Format** pairs it supports; the **Conversion matrix** is the union of those declarations.
- A **Donut** has two **Faces**, each a ring of **Slices**; the same **Donut** component is hosted in-app (1.0) or globally (1.2).
- The **Sentinel** only exists for the **Global Donut**; the **In-app Donut** needs no extra process.
- The **Broker** is the only component that touches **Source files** and **Output files**; a **Worker** receives handles, never paths.
