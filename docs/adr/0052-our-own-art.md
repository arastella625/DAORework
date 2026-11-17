# ADR-0052: Our own art: where it's made, how it's exported, and the headline marks

- **Status:** Proposed
- **Date:** 2026-11-16
- **Amends:** [ADR-0026](0026-workflow-first-layout.md) §4 (how a bitmap ornament is drawn, the seal, and *Korcari*);
  builds on [ADR-0050](0050-a-calmer-window.md) §1 (the headline card)

## Context
ADR-0026 §4 shows readiness as a seal and draws bitmap ornaments as white masks painted with a token brush, with no
ornaments in *Korcari*. ADR-0050 §1 puts five headlines on one Home card: *Welcome*, *One thing first*, *Not ready
yet*, *Ready to play* and *Ready to play, without your mods*.

The owner wanted a light medieval touch, not a new art style, and isn't a fan of the seal. So we made our own pieces
in Blender: a corner clasp, a tooled-leather band and a set of headline marks on a shield, under `art/blender/`. Over
nine passes (`art/blender/ART-PASSES.md`) the owner chose each shape and look, from what each mark says at 24 px to
how the shield was built, its surfaces, its age, and how the mark and the shield belong together.

No ADR yet says:
- where art sources live and whether they're committed;
- how a modelled render becomes something every theme can tint (a white mask keeps only the outline, so a mark that
  lives inside its shield disappears);
- how one piece works at 24 px and at 128 px;
- what *Korcari*, which draws no shading, shows when a mark's meaning is in its shading;
- how to keep pieces made at different times looking like one set.

The passes answered these by trying them:
- one picture shrunk to 24 px loses its mark under the planks and nails;
- *Korcari* showed five identical blank shields;
- pieces only look like a set when they share one light.

## Decision
1. **Sources live in the repository**, under `art/blender/`: the `.blend` files, `rig.blend`, `export_all.py`,
   `ART-NOTES.md` and `ART-PASSES.md`.
   - `renders/`, `export/`, Blender's `*.blend1` and our `*.before-*.blend` backups are ignored.
   - Made with Blender 5.3; any later version that opens them will do.
   - It's our own work, under the repository's licence. ADR-0026 §5 still holds: nothing from BioWare, nothing
     traced, nothing from a pack whose licence forbids it.
2. **One light, one export.**
   - `rig.blend` holds the light and the world every piece links: from the top left, and from above for the band. A
     piece can't drift on its own.
   - `export_all.py` opens each piece, runs that piece's own render script, and writes the masks.
   - The App keeps its copies under `src/DAORework.App/Assets/Ornaments/`, only at the sizes it draws, each at 1x and
     2x. The PR that changes a source also carries the new masks.
3. **A mask is grey with alpha** (amends ADR-0026 §4's "white masks").
   - Alpha is the shape, painted with a token brush through `OpacityMask`, as now.
   - Grey is light and shade around 50%, laid over the tint with hard light. The theme's colour is kept and so is the
     modelling: 50% leaves the tint alone, lighter lightens it, darker darkens it.
4. **Three drawings of each mark, picked by size.**
   - **Big**, above 48 px: the planked shield with its iron, nails and every fine detail.
   - **Small**, 48 px and below: a plain shield with no fine detail, so the mark is the clearest thing.
   - **Bare**: the mark alone, cropped to its own shape, with lit openings left as holes (see 6).

   The render script handles all three. An object marked `Fine detail` is left out of the small and bare drawings;
   one marked `Hole when bare` is left out of the bare one.
5. **The headline marks replace the seal** on ADR-0050 §1's card:

   | Headline | Mark |
   |---|---|
   | *Welcome* | Double doors thrown open, the doorway lit from inside |
   | *One thing first* | A signpost with one arm |
   | *Not ready yet* | The ! as a hammered nail |
   | *Ready to play* | A lit lantern |
   | *Ready to play, without your mods* | A bare candle, lit |

   - **Light means good:** the three that let you play glow; the two that ask something of you don't.
   - Any two marks read apart by shape alone at 24 px, and each sits on the shield's middle by its whole shape.
   - Each is tinted with its headline's token (heading, accent text, warning, success, secondary text). Red stays for
     danger only (ADR-0016).
6. **Korcari shows the bare marks, flat** (amends ADR-0026 §4's "*Korcari* has no ornaments"). The headline mark is
   how the card says its state, not decoration. Korcari still has no clasps, band, glow or shading.
7. **The art rules** (from `art/blender/ART-NOTES.md`):
   - stay grey, because the theme brings the colour;
   - keep the background transparent;
   - take the light from `rig.blend`;
   - check every piece at 24 px;
   - the shield and the mark on it contrast (different wood, blackened iron on the mark), while things one smith
     would make for both (the nails) stay the same;
   - the owner makes the creative calls;
   - a PR that changes art shows a sheet of the masks tinted the App's way, at their real sizes, in every look.

## Consequences
- Changing a mark is a `.blend` edit and one command. It needs no App code.
- The repository gains about 0.6 MB of `.blend` sources. The App gains only the masks it draws: the 120 masks
  `export_all.py` writes today come to 1.4 MB, and the App needs a small part of that.
- CI doesn't run Blender, so the masks are committed, not built. A test checks that every mask a view uses exists, is
  grey with alpha, and has the size its name says, beside `ViewsUseTokensTests`.
- Hard light needs Avalonia's bitmap blending. The first PR proves it in a headless render test. Hard light is
  Multiply below 50% and Screen above it, so if Avalonia can't do it in one layer, the grey is split into two masks
  drawn with Multiply and Screen, to the same result.
- `.blend` files can't be diffed in review. `ART-PASSES.md` says what each change did, and the PR shows before and
  after sheets.
- `THIRD-PARTY-NOTICES.md` doesn't change, because the art is our own.
- The marks land with P2.10-02, the headline card. The clasps and band follow this ADR when a roadmap task takes them.
  The Mods rows' small status marks and ADR-0050 §3's crest for a mod with no picture can use the same way later.

## Alternatives considered
- **Keep the seal:** the owner isn't a fan, and a tick on a disc says only "ready".
- **Vector `Path` marks only:** sharp at every size, but they can't carry wood, iron and light, which are the
  medieval touch.
- **White masks, as ADR-0026 §4 has them:** a modelled render becomes a flat outline, and a mark inside its shield
  disappears.
- **Full-colour renders:** every theme and custom look (ADR-0017) brings its own colour, and a fixed colour would
  fight all of them.
- **Sources outside the repository:** nobody else could review or remake the art, and the masks would drift from
  their sources.
- **Building the masks in CI:** it needs Blender in CI (hundreds of megabytes, minutes a run) for files that change
  rarely.
- **One picture scaled to every size:** tried in Pass 2; the planks and nails swallow the mark at 24 px.
- **No marks in Korcari:** its players would lose the card's picture of their state, not just decoration.
