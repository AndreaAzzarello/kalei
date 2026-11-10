# Kalei — Design brief

Subject: a private, local file converter for non-technical people. Its one memorable thing is the **kaleidoscope accent**: the whole window quietly takes the colour of what you drop. Everything else stays calm and neutral. Flow and states: [app-flow.md](app-flow.md). Terms: [GLOSSARY.md](../GLOSSARY.md).

## Principles
1. Neutral surfaces, one accent. The Category colour appears only on: the icon, the Drop zone border, the primary button, the progress bar, the focus ring.
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

Category accents (ratio vs `bg` / button text `on-accent` on the accent):

| Category | Light | Ratio | Dark | Ratio |
|---|---|---|---|---|
| Image | `#6A45DE` | 5.5 / 6.0 | `#AB95FF` | 7.4 / 7.4 |
| Audio | `#B02C75` | 5.6 / 6.1 | `#F58AC0` | 8.1 / 8.1 |
| Video | `#BE3A1D` | 5.1 / 5.5 | `#FF8F70` | 8.2 / 8.2 |
| Document | `#1D5DBA` | 5.8 / 6.3 | `#86B6FF` | 8.8 / 8.8 |
| Spreadsheet | `#1A7443` | 5.3 / 5.8 | `#62D493` | 9.9 / 9.9 |
| Archive | `#8A5700` | 5.6 / 6.1 | `#E8B450` | 9.6 / 9.6 |
| Other | `#4E5D6B` | 6.2 / 6.8 | `#AAB6C2` | 8.8 / 8.8 |

Empty state uses the **Other** accent in a desaturated step (icon at 40% opacity). Error uses `text` with a `#B3261E` (light) / `#FF8A80` (dark) message icon; both verified ≥ 4.5:1 when the token set is finalised in the prototype.
The set must remain distinguishable for common colour-vision deficiencies; icon glyph and label carry the meaning anyway.

## Typography
- **UI text**: Segoe UI Variable (already on Windows 11; Segoe UI fallback). Zero bytes added to the installer and native rendering.
- **Logotype only**: one bundled open-licence face (candidates below), subset to the letters of "Kalei" to keep it a few KB. Licence to be confirmed OFL in the TRD.
- Scale (px): 12 caption · 14 body · 16 control · 20 section · 28 state title. Weights 400 body, 600 titles and buttons. Line height 1.45; line length under 70 characters.
- Sentence case everywhere; no all-caps labels.

## Shape, space, motion
- Radius: Drop zone 16, buttons 10, chips and fields 8, progress track 4. Larger radius for larger objects.
- Spacing on a 4 px grid: 8, 12, 16, 24, 32. Window 560×640 default, minimum 480×560.
- Buttons: height 40; primary filled with the accent; secondary 1 px border, transparent. Focus ring 2 px accent + 2 px offset.
- **Accent transition**: 240 ms, ease-out, colour cross-fade; the icon rotates one 60° step (seamless because of its 6-fold symmetry). Turned off by the Settings toggle or Windows "Reduce animations": instant change.
- Progress bar: determinate, accent-coloured, 8 px high, no shimmer. Done: the bar fills, the icon does one 60° turn, then rests.

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
│  Convert to  [ WebP ▾ ]   ≈ 340 KB       │  appears after a drop; most common preselected
│  ▸ Advanced                              │
│                                  [Convert]│  primary, accent
└──────────────────────────────────────────┘
```
Content is left-aligned except the Drop zone message (centred). After a drop, the Drop zone shrinks into a file card (name, real Format, size, preview) and the target row appears below. In a Batch the card becomes a list with one status per row.

## Icon: three variants (all legible at 16×16)
Rule: at 16 px at most three shapes, no feature thinner than 2 px; the accent colour fills the icon.

- **A. Prism** — six 60° mirrored wedges in a hexagon, alternating accent and a lighter tint of it. At 16 px collapses to three accent wedges. Rotates in 60° steps. *Recommended: the most literal kaleidoscope, and the symmetry makes the colour-change rotation seamless.*
- **B. Bloom** — three rounded petals rotated 120° around a small centre dot; softer, friendlier. At 16 px three blobs and a dot.
- **C. Tile** — rounded square split into four mirrored triangles pointing to the centre; reads well as an app tile and in the taskbar, less "kaleidoscope" at a glance.

## Logotype: three variants
Lowercase "kalei" reads friendlier and shortens the mark; the *k* and *l* ascenders echo the wedges.

- **A. Bricolage Grotesque 600**, lowercase, tracking −2%, tight and slightly quirky.
- **B. Outfit 500**, lowercase, tracking 0, geometric and calm (round *a* and *e* echo the icon).
- **C. Fraunces 600**, "Kalei" capitalised, tracking −1%, softer, with a human feel; the only serif option.

Recommendation: icon A + logotype B (calm, geometric, consistent with the radial icon). Candidate fonts are open-licence; to be verified (OFL) before bundling. To be shown in the prototype next to each other at 16, 24 and 48 px.

## Writing (voice)
Plain verbs, sentence case, no apologies. Errors say what happened and what to do: "The file is damaged. Try another copy." "File too large (limit 2 GB). Change the limit in Settings → Advanced." Empty state: "Drop files or folders here".

## Open points for the prototype
Quiet (neutral surfaces, only the accent changes) vs Prism (the Drop zone takes a light tint of the Category). Both must keep text contrast ≥ 4.5:1 on the tinted area.
