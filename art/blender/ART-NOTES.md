# Ornament starters

Starting shapes for the medieval touch, to shape however you like. Nothing here is in the app yet.
[ADR-0052](../../docs/adr/0052-our-own-art.md) says why these sources live in the repository and how they're exported;
`renders/`, `export/` and backups stay out of it.

| File | What it is | Save the renders with |
|---|---|---|
| `clasp.blend` | Iron corner bracket over mitred oak, with three rose-head rivets (the corner one big) | `render_corners.py` (all four corners) |
| `band.blend` | Tooled-leather strip for behind the header, one repeating tile, worn along the middle, hand-stitched with one mend | `render_band.py` |
| `marks.blend` | Three replacements for the seal: Shield, Roundel, Banner, each with a tick (ready) and a ! (needs a look). The Shield also has an archway (*Welcome*) and an arrow (*One thing first*), and Pass 1's options beside them. The picks are the open door, the signpost, the nail !, the lantern and the bare candle (see ART-PASSES.md); the lantern is the one switched on. The Roundel carries the lantern and the nail ! for the Mods tiles (Pass 10) | `render_all_marks.py` (every mark and the plain shield, three times each: full, `-small` and `-bare`; the plain one has no bare) |
| `rig.blend` | The light every piece shares: *Light: top left* (clasp, marks), *Light: from above (band)*, and the world *Soft ambient*. The pieces link it, so change the light here | nothing to render |
| `export_all.py` | Renders every piece, then writes the shading masks the app tints to `export/` (`<piece>@<size>.png` and `@<size>@2x.png`, size in device-independent pixels) | `blender -b --factory-startup --python-exit-code 1 --python export_all.py` |

To run one: open the Scripting tab, pick the script in the text editor's list, then Run Script. Renders land in
`renders/` next to each file. F12 is fine for a quick look, but it only shows the render, it doesn't save it. These
notes are also inside every file as the text `READ ME`.

To render and export everything at once, close Blender and run `export_all.py` from a terminal (the line in the table).

## What the pieces are for
A medieval touch, not a new art style. The window should still feel calm (0.8.0 is the "makes sense" release), and
the words and buttons stay the main thing. An ornament earns its place if it makes the window feel like a well-kept
book or a smith's work, and nobody has to look at it to use the app.

## Rules that keep the art usable
- **Stay grey.** The app paints each theme's colour over the shape (gold in Witch of the Wilds, bronze in Blood and
  Shadow, lavender in The Fade, none in Korcari). Colour in the material is thrown away, but light and shadow are kept.
- **Keep the background transparent.** The shape's outline becomes the mask.
- **Keep the light where it is**, top left (from above for the band). Every piece in the window must agree on where
  the light comes from, or they look pasted on. The suns are linked from `rig.blend`, so a piece can't drift on its
  own; to change the light, change it there.
- **Check it small.** The clasp shows at about 34 px, a mark at about 24 to 46 px, the band at 80 px tall. Detail
  under about 1/40 of the piece disappears. Bold shapes and a clear silhouette read best.
- **Nothing from Dragon Age.** No griffons, Grey Warden heraldry, Chantry sunburst or anything traced from the games
  (LEGAL_BOUNDARIES.md). Our own shapes, or classic medieval motifs nobody owns.
- **Red stays for danger only.** Shapes are grey anyway, but don't plan a piece around being red.

## How the files are built (so the edits stay easy)
- Each plate is a flat **outline** with two modifiers: *Thickness* (Solidify) and *Rounded edges* (Bevel). Reshape it
  by moving the outline's points in Edit Mode, and the depth and edges follow. Each plate's *rim* is its own object;
  if you change the outline a lot, delete the rim and inset a new one (Edit Mode, I).
- Ticks, the !, lines and the band's motif are **curves**. Move their points, or set *Bevel > Depth* for a thicker
  stroke.
- Rivets and studs are squashed spheres. Duplicate them (Shift D) for more.
- In marks.blend the Shield has two bodies: *big (planks)* and *small (plain)*. Tick an object's custom property
  `Fine detail` (Object Properties > Custom Properties) to keep it out of the small render.
  Each mark is also rendered *bare* (no body) for Korcari; an object with `Hole when bare` is left out of that render.
  Only a body collection named *big* or *small* is rendered. The Roundel has only *small*; its other fields (*other*,
  *old*) wait there, so renaming one to *small* swaps it in.
- The band's motif and studs repeat with an *Array* modifier: edit the first tile on the left and the other eight
  follow. The leather grain repeats exactly every 4 units, so the render tiles with no seam. Keep the frame 4 wide.

## Ideas to try
**Clasp**
- A trefoil or fleur end in place of the spear tip (add points to the outline's two tips).
- Scroll the corner (a curl where the arms meet), or cut a small opening through the corner plate.
- Fewer rivets read better small; one big corner rivet may be enough.

**Band**
- Swap the lozenges for a vine or knotwork motif, which suits Witch of the Wilds; draw it as a curve in the first tile.
- Make it quieter: shallower grain (the *Bump* strength in the leather material), or the motif only near *Play*.
- Try it as a narrow strip (a belt) under the tabs instead of behind the whole header.

**Readiness marks** (instead of the seal)
- *Shield*: a heater shield with a tick, which reads "protected, all good".
- *Roundel*: an iron boss, the quietest of the three.
- *Banner*: a hanging pennant, which could sway a little if it ever moves.
- Not built yet, and worth a sketch: a lantern or candle that is lit when ready and dim when something needs a look;
  a key; a quill's tick on a scrap of parchment.
- Whichever wins, it needs both states to read apart at 24 px by shape alone (the colour helps, but can't be the
  only difference).
