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
A group of **Formats** shown with one accent colour and icon: image, audio, video, document, spreadsheet, archive, other. Each **Format** belongs to exactly one **Category** (table in the [conversion matrix](docs/conversion-matrix.md)); the **Module** does not decide it. "Other" is also the neutral colour of an empty window.
_Avoid_: Family (that is a **Module** concept), kind

**Batch**:
A set of **Source files** (including those found in dropped folders) converted in one go. In 1.0 it has one target **Format** for all its files; from 1.1, one per **Category**.
_Avoid_: Bulk, queue (the queue is the internal ordering of a **Batch**)

**Conversion matrix**:
The documented table of every supported *from Format → to Format* pair and the **Module** that handles it.
_Avoid_: Support table

**Options**:
The per-**Conversion** settings (quality, resolution, bitrate, sheets…) shown in the "Advanced" panel of a **Conversion**.
_Avoid_: Settings (those are app-wide; the editable **Limits** live in Settings → Limits)

**Limits**:
The maximum file size, pixel count, file count, folder depth, memory and time a **Conversion** may use.
_Avoid_: Quotas

### Architecture

**Core**:
The light part of Kalei that detects **Formats**, owns the queue, enforces security and manages **Modules**. It never converts anything itself.
_Avoid_: Engine, backend

**Module**:
A logical converter for one family of **Formats**: a manifest plus one or more **Engines**. The built-in Modules are hosted together in one **Worker executable**; each Optional module (from 1.1) has its own.
_Avoid_: Plugin, extension, add-on

**Worker executable**:
The binary that hosts one or more **Modules**; it is started once per **Conversion**, and that running process is a **Worker**.
_Avoid_: Module (a Module is the logical converter, not the file)

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
The running, sandboxed process, one per **Conversion**, started from a **Worker executable**.
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

**Platform adapter**:
An interface in the **Core** with one implementation per operating system (sandbox launcher, system image codec, global pointer hook, paths, shell integration, file origin, installer layout). The **Core** never calls an OS API directly.
_Avoid_: Driver, plugin

**Capability table**:
The table in [architecture.md](docs/architecture.md#capability-table-per-os) that says, for each OS-dependent element, how each operating system provides it or that it is not available.
_Avoid_: Compatibility matrix (that would be confused with the **Conversion matrix**)

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
The approximate output size shown before converting. Not in 1.0; undecided for later.
_Avoid_: Prediction

**Bloom**:
Kalei's icon: three petals around a centre dot that takes the **Accent**. Bare inside the app, in a rounded container as the app icon.
_Avoid_: Logo (the logotype is the word "kalei")

**Hub**:
The centre of the **Donut**, showing the Bloom and the hint to flip the **Face**.
_Avoid_: Centre button

## Relationships

- A **Batch** contains one or more **Conversions**.
- Each **Conversion** runs in exactly one **Worker**, which belongs to exactly one **Module**.
- A **Module** wraps one or more **Engines** and declares which **Format** pairs it supports; the **Conversion matrix** is the union of those declarations.
- A **Donut** has one or two **Faces** (the back exists only when there are more targets than fit on the front), each a ring of **Slices**; the same **Donut** component is hosted in-app (1.0) or globally (1.2).
- The **Sentinel** only exists for the **Global Donut**; the **In-app Donut** needs no extra process.
- The **Broker** is the only component that touches **Source files** and **Output files**; a **Worker** receives handles, never paths.
