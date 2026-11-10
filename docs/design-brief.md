# Kalei — Design brief

Subject: a private, local file converter for non-technical people. Its one memorable thing is the **kaleidoscope accent**: the whole window quietly takes the colour of what you drop. Everything else stays calm and neutral. Flow and states: [app-flow.md](app-flow.md). Terms: [GLOSSARY.md](../GLOSSARY.md). Decisions come first in this file; the options that were considered and dropped are in the appendix.

## Decisions at a glance
- **Layout**: A "Quiet": neutral surfaces, only the accent changes. Chosen in the 2026-11-10 design review (HISTORY).
- **Logotype**: "kalei", lowercase, Bricolage Grotesque 600, embedded in the app. Licence OFL 1.1, confirmed from upstream; see the [TRD](TRD.md) for licence text, the Mayenne Sans caveat and the definition of the embedded subset.
- **Icon**: the Bloom (three petals around a centre dot). In the app header it is bare; as the app icon it sits in a rounded square, container `#6C5FF1` (contrast table below).
- **Donut**: ring of Slices, see below.

## Principles
1. Neutral surfaces, one accent. The Category colour appears only on: the icon's centre dot, the Drop zone border, the primary button, the progress bar, the focus ring, and in the Donut the Slice under the cursor and the hub's dot.
2. No gradients, glass, or shadows as decoration. Depth comes from one surface step (background → surface) and a 1 px border.
3. Words are the interface: sentence case, active verbs, same name for an action through the whole flow ("Convert" → "Converted").
4. Colour is never the only signal: the Category name and icon glyph are always shown.
5. Motion only answers an action (drop, convert, finish).

## Colour tokens

Surfaces and text are neutral, with a slight warmth in light mode. Contrast was computed with the WCAG 2.x formula (script run during this task); every pair below is ≥ 4.5:1.

| Token | Light | Dark |
|---|---|---|
| `bg` | `#F6F5F3` | `#151518` |
| `surface` | `#FFFFFF` | `#1F1F24` |
| `text` | `#1B1B1F` (15.8:1 on bg) | `#EDEDF0` (15.6:1) |
| `muted` | `#5B5B63` (6.2:1) | `#A3A3AD` (7.3:1) |
| `on-accent` | `#FFFFFF` | `#151518` |

Category accents (ratio vs `bg` / button text `on-accent` on the accent). **Used in 1.0: image, document, spreadsheet** (plus "other" as the neutral empty state); audio, video and archive are reserved for later Modules.

| Category | Light | Ratio | Dark | Ratio |
|---|---|---|---|---|
| Image | `#6A45DE` | 5.5 / 6.0 | `#AB95FF` | 7.4 / 7.4 |
| Audio *(later)* | `#B02C75` | 5.6 / 6.1 | `#F58AC0` | 8.1 / 8.1 |
| Video *(later)* | `#BE3A1D` | 5.1 / 5.5 | `#FF8F70` | 8.2 / 8.2 |
| Document | `#1D5DBA` | 5.8 / 6.3 | `#86B6FF` | 8.8 / 8.8 |
| Spreadsheet | `#1A7443` | 5.3 / 5.8 | `#62D493` | 9.9 / 9.9 |
| Archive *(later)* | `#8A5700` | 5.6 / 6.1 | `#E8B450` | 9.6 / 9.6 |
| Other | `#4E5D6B` | 6.2 / 6.8 | `#AAB6C2` | 8.8 / 8.8 |

Empty state uses the **Other** accent in a desaturated step (icon at 40% opacity). Error uses `text` with a `#B3261E` (light) / `#FF8A80` (dark) message icon; both verified ≥ 4.5:1 when the token set is finalised in the prototype.
The set must remain distinguishable for common colour-vision deficiencies; icon glyph and label carry the meaning anyway.

## Typography
- **UI text**: Segoe UI Variable (already on Windows 11; Segoe UI fallback). Zero bytes added to the installer and native rendering. On other systems the platform's system font.
- **Logotype and Donut labels**: Bricolage Grotesque 600, embedded as a subset defined once in the [TRD](TRD.md) (never loaded from the network).
- Scale (px): 12 caption · 14 body · 16 control · 20 section · 28 state title. Weights 400 body, 600 titles and buttons. Line height 1.45; line length under 70 characters.
- Sentence case everywhere; no all-caps labels (format names such as PNG are names, not labels).

## Shape, space, motion
- Radius: Drop zone 16, buttons 10, chips and fields 8, progress track 4. Larger radius for larger objects.
- Spacing on a 4 px grid: 8, 12, 16, 24, 32. Window 560×640 default, minimum 480×560.
- Buttons: height 40; primary filled with the accent; secondary 1 px border, transparent. Focus ring 2 px accent + 2 px offset.
- **Accent transition**: 240 ms, ease-out, colour cross-fade; the icon rotates one 120° step (seamless because the Bloom has three identical petals). **Precedence**: Windows "Reduce animations" on, or Kalei's "Colour transition" off, means an instant change; the stricter setting wins.
- Progress bar: determinate, accent-coloured, 8 px high, no shimmer. Done: the bar fills, the icon does one 120° turn, then rests.

## Layout (one window)

```
┌──────────────────────────────────────────┐
│  [icon] kalei                        ⚙   │  header: icon + logotype left, Settings right
│                                          │
│  ┌ · · · · · · · · · · · · · · · · · ┐   │
│  :                                    :  │  Drop zone: 16 px radius dashed accent border
│  :     Drop files or folders here     :  │  centred, 40% of height
│  :       [ Choose files ]             :  │
│  └ · · · · · · · · · · · · · · · · · ┘   │
│                                          │
│  Convert to  [ WebP ▾ ]                  │  appears after a drop; most common preselected
│  ▸ Advanced                              │
│                                  [Convert]│  primary, accent
└──────────────────────────────────────────┘
```
Content is left-aligned except the Drop zone message (centred). After a drop, the Drop zone shrinks into a file card (name, real Format, size, preview) and the target row appears below. In a Batch the card becomes a list with one status per row and one target Format for the whole Batch (1.0).

## Donut
- Ring of Slices; neutral Slices (surface colour + 1 px border), only the Slice under the cursor takes the Category accent; the hub (centre of the ring) shows the Bloom without container with its centre dot in the Category accent. Labels in Bricolage Grotesque.
- Disabled Slices (from 1.1, Module missing) at 40% opacity with the reason as text, never colour alone.
- Opening: scale/fade from the cursor in 160 ms; Face flip (only when a back Face exists, not in 1.0): 200 ms turn about the vertical axis. Both instant under the precedence rule above.
- Contrast rules as for the rest of the app (≥ 4.5:1 text, ≥ 3:1 graphics).

### Donut faces
Targets are taken from the [matrix](conversion-matrix.md) with the Source Format excluded. Up to 8 targets on the front Face, ordered by how common they are; a back Face exists only when more than 8 remain, and the "Mouse wheel: more formats" hint is hidden when there is none.

| Category (in 1.0) | Front Face, in order (Source excluded) | Back Face |
|---|---|---|
| image | WebP, PNG, JPG, AVIF, GIF, TIFF, BMP, ICO (up to 7 after excluding the Source) | none |
| spreadsheet | CSV, XLSX, JSON, TSV | none |
| document | for JSON, YAML, XML: JSON, YAML, XML, CSV, TSV, XLSX (JSON only for the tabular ones); for TXT, MD, HTML: TXT, MD, HTML | none |

Mixed files: only the Formats that every file can be converted to or already is. The back Face and its rotation arrive with the larger Modules (audio, video).

## App icon contrast (fix, 2026-11-10)
Problem: the first container `#4B3FD1` had 2.27:1 on a dark taskbar, under WCAG 1.4.11 (3:1). The `.ico` cannot follow the theme, so **one** container colour must work on both. Taskbar colours used are **assumptions, not measurements** (Windows 11 taskbars are translucent and depend on wallpaper and accent): dark `#202020`, light `#F3F3F3`, plus a sweep dark `#101010`–`#2B2B2B` and light `#E6E6E6`–`#FFFFFF`.

| | Before `#4B3FD1` | After `#6C5FF1` |
|---|---|---|
| vs dark taskbar `#101010` / `#202020` / `#2B2B2B` | 2.66 / **2.27** / **1.98** | 4.12 / 3.53 / **3.06** |
| vs light taskbar `#E6E6E6` / `#F3F3F3` / `#FFFFFF` | 5.74 / 6.46 / 7.17 | 3.70 / 4.16 / 4.62 |
| White petals / container (needs ≥ 4.5) | 7.17 | 4.62 |
| Dot / container (needs ≥ 3) | 4.26 (`#FFB0DC`) | 3.58 (`#FFE08A`) |
| Dot / white petals | 1.68 (**problem**) | 1.29, solved by a container-coloured halo around the dot |

The feasible window is narrow: white petals need container luminance ≤ 0.183 and the lightest dark taskbar in the sweep needs ≥ 0.173; `#6C5FF1` sits at 0.177, so the worst case (`#2B2B2B`) is 3.06:1, just above 3:1. The first design also hid a flaw: the pink dot sat directly on white petals (1.68:1); the new icon draws the dot on a ring of container colour so the dot is always seen against the container. The other option, a darker container, fails the petal contrast; a lighter one fails the dark taskbar; there is no wider margin with white petals. Sources: `assets/icon/kalei-app-icon.svg` (32/48/256 px) and `kalei-app-icon-small.svg` (hand-tuned 16/24 px), packed into `assets/icon/kalei.ico` by `assets/icon/build-icon.mjs`. 16 and 24 px use narrower, more spaced petals. The final judgement on a real taskbar is the owner's.

## Writing (voice)
Plain verbs, sentence case, no apologies. Errors say what happened and what to do: "The file is damaged. Try another copy." "File too large (limit 2 GB). Change the limit in Settings → Limits." Empty state: "Drop files or folders here".

## Appendix — options considered and not chosen (historical)
Kept so the reasoning is not lost; none of this is current.
- **Icon variants**: A *Prism* (six 60° mirrored wedges in a hexagon, the most literal kaleidoscope; it was the original recommendation), B *Bloom* (chosen), C *Tile* (rounded square of four mirrored triangles; its rounded-square container is reused for the app icon).
- **Logotype variants**: A Bricolage Grotesque 600 (chosen), B Outfit 500, C Fraunces 600. The original recommendation (icon A + logotype B) was superseded by the choices above.
- **Layout**: A *Quiet* (chosen) against B *Prism*, in which the Drop zone takes a light tint of the Category. B was dropped because a tinted zone makes legibility of text and of the Donut harder.
- Prototype of these options: branch `prototype/ui-variants`, screenshots in `prototype-screenshots/`.
