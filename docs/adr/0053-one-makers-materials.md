# ADR-0053: One maker's materials: shared surface recipes for the art

- **Status:** Proposed
- **Date:** 2026-11-22
- **Amends:** [ADR-0052](0052-our-own-art.md) §2 (one light, one export) and §7 (the art rules)

## Context
ADR-0052 §2 makes the light one thing: `rig.blend` holds it, and every piece links it, so no piece can drift. The
surfaces have no such rule, and they have started to drift:
- Pass 10 made the Roundel's polished iron as a copy of the shield's *Hammered iron grey*.
- Pass 11 copied that iron and the rose-head nail into `clasp.blend`, and added the shield's adze scoops to the clasp's
  oak by rebuilding the same nodes by hand.
- A change to the smith's iron in `marks.blend` now reaches neither copy. The Banner (Pass 14) would add a fourth piece
  and more copies.

Pass 6 settled what should match: whatever one maker would make for every piece (the smith's iron and nails, the hewn
oak, the tooled leather) is the same thing everywhere, while a field and the charge on it must differ. So the pieces
need to share a surface and still vary it.

Blender can link a material from another file, but a linked material can't be changed in the file that links it, so
every variant would be a local copy again. Blender can also link a *node group*, a reusable piece of a material with
inputs, and a local material can use it with its own input values.

## Decision
1. **`art/blender/materials.blend` holds the shared surfaces**, next to `rig.blend`, as node groups with named inputs:
   - *Smith's iron*: hammer dents, polish on the edges, grime, pits. Its inputs are lightness, polish, dent depth and
     grip rub, so the shield's hammered iron, the marks' blackened iron and the Roundel's polished iron are one recipe.
   - *Hewn oak*: grain, fibres, adze scoops, checks, old paint. Its grain directions come in as inputs, so the shield's
     planks and the clasp's mitred boards both use it.
   - *Tooled leather* and, for the Banner, *Linen*.
   - The shared meshes one maker would reuse, starting with the rose-head nail.
2. **A surface goes in `materials.blend` when two pieces use it, or when one maker would make it for both.** A surface
   only one piece has (the stone, the wax, the light from inside, the rust streaks) stays in that piece's file.
3. **Each piece links what it needs, the way it links the light.** Its own materials are thin: one linked group and the
   values that make this piece's version, named for what they are (*Polished iron grey (the roundel)* is *Smith's iron*
   with more polish and lightness). Nobody makes a linked group local; to change a recipe, open `materials.blend`.
4. **Links are relative** (`//materials.blend`, like `//rig.blend`), so a fresh clone renders with no setup.
5. **A missing link stops the export.** `export_all.py` checks that every linked light, group and mesh was found
   before it renders, and fails with the missing name instead of rendering a pink surface into the masks.
6. **Moving to it changes no picture.** Pass 12 moves today's materials into the groups, and every render before and
   after must match within render noise, as Pass 8 proved for the light. Anything that changes the look is a later pass.
7. **The art rules** (ADR-0052 §7) gain one: *take the light from `rig.blend` and the shared surfaces from
   `materials.blend`*.

## Consequences
- A change to the smith's iron, the oak or the leather is made once and reaches every piece the next time it renders.
- A piece's own look stays visible and editable in its own file, as a handful of input values.
- Editing a recipe means opening `materials.blend`; in a piece's file the group is read-only. `ART-NOTES.md` says so,
  next to the same note for the light.
- One more `.blend` in the repository (a few hundred KB at most). The App, the masks and CI don't change.
- The Pass 10 and 11 copies (*Hammered iron grey (the smith's, from marks.blend)*, *Rose head nail (from
  marks.blend)*, the clasp's hand-copied adze scoops) are replaced by links in Pass 12.
- The Banner (Pass 14) is built on the shared groups from the start, so its linen and iron never exist as copies.
- A missing or renamed `materials.blend` fails the export by name instead of making wrong masks quietly.

## Alternatives considered
- **Keep copies in each file:** the drift has already started after two passes, and each new piece would add more.
- **Link whole materials:** a linked material can't be varied in the file that links it, so every variant (polished,
  blackened, the clasp's mitred grain) would be a local copy again.
- **Put the surfaces in `rig.blend`:** one library fewer, but "change the light here" would stop being the whole story
  of that file. One file per job keeps both rules short.
- **Blender's Asset Browser library:** it needs a library folder set in each person's Blender preferences, so a fresh
  clone wouldn't render without setup.
- **A script that re-copies the surfaces into every file:** it only works when someone remembers to run it, and a
  hand edit to a copy would be overwritten without warning.
