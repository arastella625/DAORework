# Passes over the starters

The starters are blockouts: they have the right shapes, but nothing says what they are made of or what they mean.
These passes take them from "grey shapes" to "things somebody made". Each pass makes one kind of change to every
piece and ends with a render sheet. You pick what stays before the next pass starts, so a later pass never builds on
a choice you haven't made.

They go from big to small, the way a painter works: what it *says*, then what it's *built* from, then its *surface*,
then its *age*, then *who made it* and *what it's been through*, then whether it *fits the window*. Changing the silhouette after the wear is painted means painting
the wear again.

## Pass 0: What each piece is made of
One line per piece, decided before any texture. The theme paints its own colour over everything, so two materials
can only look different through **light, roughness and pattern**, never through hue. Iron next to wood reads because
iron is smooth with sharp highlights and wood is matte with grain.

| Piece | Made of (suggestion) |
|---|---|
| Clasp | Black iron strap over aged oak (done in your clasp.blend) |
| Band | Tooled leather, stitched edges (done) |
| Shield | Oak planks with an iron rim; the mark is iron nailed on |
| Roundel | One hammered iron boss with a flange and rivets |
| Banner | Heavy linen on a crossbar; the mark is stitched in |

One shared `materials.blend` (iron, oak, leather, linen) that every file links from, so a change to "iron" changes it
everywhere and the pieces always look like a set.

## Pass 1: Say it at 24 px (meaning and silhouette)
The archway is the warning: an arch on its own is an upside-down U. A mark has to say its word by shape alone, at the
smallest size, before anyone has learned what it means.

| Headline | Today | Suggested shape | Why it reads |
|---|---|---|---|
| Welcome | Archway | **A door standing open**, one leaf swung out, the opening lit from inside | Open door = come in; the bright opening does the talking |
| One thing first | Arrow | **A signpost with one arm** | One arm, one way to go; an arrow says "next", not "first" |
| Not ready yet | ! | **The ! as a hammered nail**, or a broken chain link | The ! is universal; the nail just makes it belong |
| Ready to play | Tick | **A chiselled tick**, or a lit lantern | The tick already works; the lantern is a warmer option |
| Ready, without your mods | Plain | **The shield with an empty hook** or a bare strap | Plain looks unfinished; "nothing hung here" is deliberate |

The test sheet: all five side by side at 24, 34 and 46 px, grey and in each theme's colour. If two can't be told
apart squinting at 24 px, change a shape, not the shading.

**Picked (2026-11-16)**, from `renders/pass1-sheet.png`. These go on to Pass 2:

| Headline | Pick | Collection in marks.blend | Still to solve |
|---|---|---|---|
| Welcome | Double doors thrown open, lit from inside | *Shield: welcome (open door)* | |
| One thing first | Signpost: the post on the middle, one arrow-shaped board | *Shield: one thing first (signpost)* | |
| Not ready yet | Nail ! | *Shield: needs a look (nail)* | |
| Ready to play | Lantern, lit | *Shield: ready (lantern)* | The chiselled tick is the runner-up if the lantern ever feels too busy |
| Ready, without your mods | A bare candle, lit (picked over the empty hook) | *Shield: without your mods (candle)* | |

Together they share one idea: **light means good**. The three ready or welcome states (door, lantern, candle) glow;
the two that ask something of you (nail, signpost) don't. The older marks (archway, arrow, !, tick, chiselled tick,
empty hook) stay in the file, switched off, in case you want them back.

**Centred (2026-11-16)**, sheet in `renders/centre-sheet.png`. A mark sits on the shield's middle by its *whole* shape,
not by its first part. A symmetric mark centres itself, so the door became double doors and the signpost's post moved
onto the middle line, carrying one arrow-shaped board with a swallowtail behind. A shape that can't be symmetric (the
hook) is moved until its outline is centred, even though its plate then sits left of the middle. Heraldry places
charges this way. A double hook was tried and dropped: at small sizes it reads as an anchor.

## Pass 2: How it's built (secondary forms)
- **Shield**: three vertical planks with grooves, a gentle curve across (real heater shields bow), the iron rim held by
  nails, a horizontal strap or a small boss behind the mark.
- **Roundel**: a raised ring, a flange with six rivets, a slight dome.
- **Banner**: real folds (a cloth sim, or a few shape keys you can push around), a crossbar with turned ends, a short
  fringe.
- **Marks** become their own material sitting *on* the body: an iron tick nailed to wood, a stitched tick on linen.
  The edge where two materials meet is what makes a thing look made.

**Done on the Shield (2026-11-16)**, sheet in `renders/pass2-sheet.png` (Pass 1 left, Pass 2 right):
- Three planks (*Shield plank left/middle/right*), each turned 4° on its inner edge so the shield bows. Turn a plank's
  Y rotation for more or less bow; the strap pieces and their nails are its children and follow.
- An iron edging band (*Shield edging*) bent where the planks meet, with *Edging nail* studs every 0.21.
- A strap across the top, one piece per plank and a nail into each.
- On the marks: a keystone and two hinge straps on the door; a longer signpost arm with a bigger point, a pointed post
  and two nails; a rose head on the nail and a washer under its dot; a crossbar, lip and knob on the lantern; a second
  plate nail and a ball end on the hook.
- The Pass 1 body and rim are in *Shield blockout (off)*, switched off. The Roundel and Banner aren't touched.

**What the sheet shows:** big, the shield now looks made. At 24 to 34 px the plank grooves and the strap compete with
the marks, and worst with the ones that are tall and thin (the nail ! and the hook), because they run the same way as
the grooves. That's the job Pass 3's two detail levels are for: the small render keeps the edging and the mark and
drops the grooves, strap and nails.

## Pass 3: Surface (textures)
Procedural node textures, value only, from the shared materials: oak grain (your clasp's *Aged wood grey*), hammered
iron (Voronoi dents in the bump), a coarse linen weave, the leather grain you already have.

The catch is scale: detail smaller than about 1/40 of the piece turns to mush at 24 px. So each file gets a
**Fine detail** collection (grain, weave, nail heads, scratches) that renders for the big uses (the headline card at
96 to 128 px, *About this mod*) and is switched off for the small render. Icon designers do the same, drawing the 16 px
icon separately instead of shrinking the big one.

**Done on the Shield (2026-11-16)**, sheet in `renders/pass3-sheet.png` (Pass 2, then Pass 3 big, then Pass 3 small):
- Four new materials, each with its knobs labelled in the node editor:
  - **Oak grey**: the planks, door leaves and signpost. Each board takes its own piece of the grain, and an object's
    `grain_angle` property turns the grain (the signpost arm has 90°, written as 1.5708).
  - **Hammered iron grey**: the edging, strap, nails and every iron part of the marks.
  - **Stone grey**: the door frame, keystone and signpost ground. An object with `stone_courses` set to 1 gets joints:
    wedge stones round the arch, courses below.
  - **Wax grey**: the candle, darker than its flame so the flame still reads.
- **Two detail levels.** *Shield body: big (planks)* holds the planks, edging, strap and nails. *Shield body: small
  (plain)* holds the Pass 1 body and rim, now in oak and iron, hidden in the viewport. Objects with the custom property
  `Fine detail` (hinge straps, rings, the signpost's nails, the rose head and washer, the lantern's lip and knob, the
  candle's wick) are left out of the small render. These are a body and a property instead of one *Fine detail*
  collection, because the fine parts belong to different marks.
- `render_all_marks.py` renders each Shield mark twice: `mark-shield-<mark>.png` big and `mark-shield-<mark>-small.png`
  small. The Roundel and Banner have no detail levels, so they render once as before.

**What the sheet shows:** small, every mark is a clean shape again, which is the point of the small level. Big, the
grain reads as wood, though it's still more wavy than real oak; a straighter grain is a one-number change (the
*Distortion* on the *Grain lines* node).

## Pass 4: Age (wear and story)
- Edges polished bright where hands would hold them (an edge mask from the Bevel node or Pointiness).
- Grime darkening the grooves and around rivets (ambient occlusion).
- One honest flaw each: a nick in the shield rim, a darned patch on the banner, one rivet replaced with a newer one.
- Keep it gentle: 0.8.0 is the calm release, so a well-kept heirloom, not a battlefield wreck.

**Done on the Shield (2026-11-16)**, sheet in `renders/pass4-sheet.png` (Pass 3, then Pass 4 big and small) and a
close look in `renders/pass4-closeup.png`. Every node this pass added has "(Pass 4)" in its label.
- **Hammered iron grey**: edges polished lighter and shinier, grime darkening the corners, and faint darker patches
  (old heat and oil). The knobs: *Edge width*, *Polish on the edges*, *Grime reach*, *Grime darkness*, *Patch depth*.
- **Oak grey** and **Stone grey**: rubbed, paler edges and darker corners, laid over their Pass 3 colour.
- Honest flaws, all tagged `Fine detail`:
  - a **nick in the edging** on the right side. It's cut by the wireframe cube *Nick in the edging (cutter, move me)*;
    move or turn the cube and the nick follows.
  - one **replaced nail** on the left, *Edging nail 05 (the new one)*: rounder, shinier, not yet dented.
  - the **signpost board has sagged** 1.5° on its nails (the arm's Z rotation).
  - a **second, shorter drip** on the candle.
- The Roundel and Banner are still untouched, so the banner's darned patch waits for them.

**What it shows:** the wear is on purpose quiet. It's clear up close, a faint warmth at the sheet's size, and gone at
24 px, which is right for the calm release. If you want more age, raise *Grime darkness* first; it reads at smaller
sizes than edge polish does.

## Pass 5: Made by hand (the medieval part)
What makes a thing look medieval isn't age, it's that **a person made it with hand tools**. Nothing is machine-straight,
nothing repeats exactly, and the smith and the carpenter left their marks. Pass 2 built the right parts; this pass makes
them look forged and hewn.
- **Nothing quite straight.** Planks of slightly different widths with gently wavering edges; the strap a touch out of
  level; nails spaced by eye, each a little different in size and turned a little.
- **Forged iron, not cast.** Every round nail becomes a four-faceted rose head. The strap ends in a smith's flourish (a
  spear point or a split curl) instead of a square cut, and the door's hinge straps end in scrolls. The edging thickens
  and thins where it was hammered round the shield.
- **Hewn wood, not sawn.** Shallow adze scoops across the planks (long Voronoi cells in the bump) and slightly rounded,
  hand-planed board edges.
- **Dressed stone.** Chisel marks across each stone of the door's arch, and the arrises (edges) softened unevenly.
- **The marks too.** The signpost's point is cut with a few straight knife facets, not one clean line; the lantern's
  frame is riveted at the corners; the candle is hand-dipped, a little thicker at the bottom.

**Done on the Shield (2026-11-16)**, sheet in `renders/pass5-sheet.png` (Pass 4, then Pass 5 big and small) and close
looks in `renders/pass5-closeup-door.png` and `renders/pass5-closeup-signpost.png`.
- **Boards:** the two joints moved (0.02 and 0.012), so no two planks are the same width, and each joint wavers a
  little, as if planed by hand.
- **Strap:** a touch out of level (it climbs 0.018 per unit to the right), joined where the planks now meet, with spear
  ends from the smith.
- **Edging:** its thickness and inner edge wander, as if hammered round the shield.
- **Nails:** every round nail is now a four-faceted rose head, each a little different in size and turn. They share one
  mesh, *Rose head nail*, so reshaping it reshapes them all. *Edging nail 05 (the new one)* stays round on purpose: a
  later smith's nail.
- **Door:** the hinge straps end in scrolls.
- **Tool marks:** adze scoops in *Oak grey* (*Adze scoops*, *Adze depth*) and chisel strokes in *Stone grey*
  (*Chisel strokes*, *Chisel depth*). Their nodes are labelled "(Pass 5)".
- **Marks:** the signpost's point is cut with a few knife strokes and its edges aren't quite straight; the candle is
  hand-dipped, thicker at the bottom; the lantern's frame has a rivet at each corner (*Fine detail*).
- Each object this pass reshaped has the property `Made by hand`, so running the pass again never doubles the change.

**What it shows:** up close it now looks forged and hewn; the spear-ended strap and scrolled hinges do the most. At
the sheet's size the uneven strap and rose heads still read; at 24 px nothing changed, as planned.

**Tweaked (2026-11-16):** the marks no longer share the shield's oak. The door leaves and the signpost are now
*Walnut grey (the marks)*: darker, finer and straighter grain, planed smooth (barely any adze) and oiled (a little
shine). It started as a copy of the oak, so its knobs have the same names. Sheet: `renders/mark-wood-sheet.png`.

## Pass 6: Related, not repeated (cohesion between the shield and its marks)
Reusing a surface ties things together, but reusing too much makes the mark melt into the shield, as the shared oak
did. Heraldry has solved this for 800 years: the **field** (the shield) and the **charge** (the mark on it) must
contrast, while one hand and one style make them a set. So this pass asks, for every surface: *should these two read
as the same thing, or as two things that belong together?*

- **Same thing, so share it:** anything one craftsman would have made for both. The smith made all the iron, so every
  nail, strap and scroll keeps the same rose heads, hammer dents and polish, on the shield and on the marks.
- **Belong together, so vary it:** surfaces that sit on top of each other. The field gets the rough, broad, lighter
  version (hewn oak, the edging's dull iron); the charge gets the finer, darker or smoother one (walnut, blackened or
  oiled iron, dressed stone). Same family, different member.
- **One echo per mark.** Each mark borrows *one* shape from the shield so they rhyme without copying: the signpost's
  point echoes the strap's spear ends; the lantern's bail could curl like the door's hinge scrolls; the nail !'s rose
  head is the shield's own nail, made big. One echo is cohesion, three is a copy.
- **Keep proportions in step.** Stroke widths, bevel sizes and nail sizes come from one small scale (say three sizes of
  nail, two strap widths), so the marks look drawn by the same hand as the shield.
- **The iron on the marks**: blacken it (darker, a little oily shine) so the lantern, nail ! and hook read as objects
  placed on the shield, not as part of its edging.
- **Two tests at the end.** *Squint*: at sheet size, every mark separates from its shield at first glance. *Swap*: put
  any mark on another body (the Roundel, the Banner) and it still belongs, which proves the cohesion comes from the
  style, not from the one shield.

**Done on the Shield (2026-11-16)**, sheet in `renders/pass6-sheet.png` (before, then Pass 6 big and small), close
looks in `renders/pass6-closeup-lantern.png`, `-candle.png` and `-nail.png`, and the swap test in
`renders/pass6-swap-test.png`.
- **Blackened iron grey (the marks):** every iron part of the five marks (lantern, nail !, candle dish and foot, the
  door's hinge straps and rings) is now darker with a little oily shine. It's a copy of *Hammered iron grey*, so the
  knobs are the same: *Polish on the edges* holds its value, *Shine on the edges* its roughness.
- **Shared, on purpose:** the smith's nails. The signpost's nails and the lantern's rivets keep the shield's hammered
  iron and the shared *Rose head nail* shape.
- **One echo per mark:**
  - the lantern's ring became a bail whose feet curl like the door's hinge scrolls;
  - the nail !'s head *is* the shield's rose-head nail, made big;
  - the candle's foot ends in two small scrolls;
  - the signpost's point already echoes the strap's spear ends, and the door is where the scrolls come from.
- **Proportions in step:** two nail sizes (small 0.026 for the edging and the lantern, medium 0.036 for the strap and
  the signpost), each nail still a little different; one thin stroke (0.014) for hinge straps, the lantern's bars and
  lip, and the washer.
- **Tests:** *squint*: every mark now separates from the shield at first glance, and at 24 px the nail ! and the
  lantern read better than before. *Swap*: all five marks belong on the Roundel and the Banner too. The door is the
  tightest fit on the Banner, where its light on the ground is cut short.

## Pass 7: Time and use (worn and old)
Pass 4 made a well-kept heirloom. This pass gives it a past: what years of use, weather and keeping would really do.
- **Weathered oak.** The soft wood between the grain wears away, so the grain stands up in ridges (much stronger grain
  relief), the wood silvers paler, and checks (thin cracks along the grain) open near the plank ends. One plank has a
  split at its bottom end, held by an extra nail.
- **Rust that runs.** Each nail leaves a faint darker streak running down the wood below it, and the iron gets pitted
  where it was rusted and scrubbed. (Value only: the streaks are darker, never orange.)
- **Old paint, mostly gone.** Shields were painted. A faded division (say, per pale, or a chevron) survives in patches,
  worn through to the wood at the edges and where hands held it. The theme reads it as a second tone, and it's the first
  sketch of *your crest*.
- **Where it was used.** The bottom point is scuffed and darker, because shields rest on the ground point first. The
  strap is rubbed bright in the middle where it was gripped.
- **The marks.** The door's threshold is worn into a dip in the middle; the signpost's board has weathered paler than its
  post; the candle has melted into a small puddle in its dish; the lantern's glass has one cracked pane.

Passes 5 and 7 go on the **big** level only, all tagged `Fine detail` where they're objects. The small level stays the
clean shape it is, so the window stays calm at 24 px and the age shows where there's room for it: the headline card and
*About this mod*. Turn any of it down if a piece starts to look like a battlefield wreck instead of something kept.

**Done on the Shield (2026-11-16)**, sheet in `renders/pass7-sheet.png` (before, then Pass 7 big and small) and close
looks in `renders/pass7-closeup-open-door.png`, `-signpost.png`, `-lantern.png` and `-candle.png`. Every node this pass
added has "(Pass 7)" in its label.
- **Weathered oak**, only where an object's `Time and use` is 1 (the three big planks; the small body has none, so it
  stays clean): the grain stands up in ridges, the wood silvers paler, and checks open near the plank ends.
- **Old paint:** a chevron, worn through in patches and gone from the edges (*Paint strength*, *Chevron width*,
  *How much paint is left*). It's the loudest new thing at the sheet's size, where it reads as a pale diagonal. Kept at
  full strength (your call, 2026-11-16); *Paint strength* is the knob if that ever changes.
- **The point** is scuffed darker, where the shield rests on the ground.
- **Iron:** pitted where it rusted (*Pits*, *Pit depth*), and the strap is rubbed bright in the middle.
- **Rust that runs:** ten faint dark streaks below the strap, its nails and four top edging nails. They're thin planes
  just above the wood using *Rust streak grey* (dark, fading as they run down); move or stretch one to move a streak.
- **A split** in the left plank's bottom end, cut by *Split in the plank (cutter, move me)*, with *Nail across the
  split* holding it.
- **The marks:** the signpost's board (with `weathered` 1) has gone paler than its post with its grain standing up;
  the door's threshold is worn into a polished dip; the candle has a wax puddle in its dish; one lantern pane is
  cracked.
- All new objects are `Fine detail`, so the small level is unchanged.

## Pass 8: One light, one export
- A *Light rig* collection kept in one file and linked into every other one, so all the pieces agree on where the light
  comes from (top left; from above for the band).
- One export script that renders every piece and both detail levels, and turns them into the shading masks the app
  tints (the step the mock already uses).

**Done (2026-11-16):**
- **`rig.blend`** holds the light: *Light: top left* (linked by clasp.blend and marks.blend), *Light: from above
  (band)* (linked by band.blend) and the world *Soft ambient* (linked by all three). Each piece's own sun and world are
  gone, and its camera collection is now *Camera (keep as is)*. Before and after renders match to within render noise
  (largest pixel difference 2 to 11 levels out of 255 on single pixels, average effectively 0).
- **`export_all.py`** opens each piece, runs its own render script, then writes 70 masks to `export/`: the four clasp
  corners at 34 and 68, the band at 80 and 160 tall, and the five picks plus the plain shield at 24, 34, 46, 48, 68 and
  92 (from the small level) and 96, 128, 192 and 256 (from the big one). The marks it exports are listed at the top of
  the script.
- Check sheet: `renders/pass8-export-check.png`, the masks tinted gold and lavender the app's way at real sizes.
- Backups from before the link, on the owner's machine only (`.gitignore` keeps `*.before-*.blend` out of the
  repository): `clasp.before-rig.blend`, `band.before-rig.blend`, `marks.before-rig.blend`.

## Pass 9: In the window
Put each pass's renders into the clickable mock in all four themes, and check the ornaments are still quieter than the
words. If anyone looks at an ornament before the headline, it's too loud.

**Done (2026-11-16)**: the clickable 0.8 mock (a private page, not in the repository) now uses
the exported masks: the clasps, the band, and the five picks as the Home card's headline marks. Checked in all four
looks.
- Witch of the Wilds, Blood and Shadow and The Fade: every mark reads at 64 px and stays quieter than the headline
  words; the blackened iron carries each mark against its tinted shield.
- **Korcari broke, and is fixed.** Korcari draws ornaments flat (no shading), and a mark inside a shield is only
  shading, so every headline showed the same blank shield. Now each mark is also rendered **bare**: no body, no fine
  detail, and lit openings left out so they read as holes (objects with the property `Hole when bare`: the doorway's
  light, its light on the ground, the lantern's glass). `export_all.py` crops each bare mark to its own shape and writes
  `mark-shield-<mark>-bare@<pixels>.png`; the mock gives Korcari those. In Korcari the marks are now five flat emblems:
  a doorway, a signpost, a !, a lantern with its panes, a lit candle.
- The rows' small status marks in the mock are still the old Roundel; the passes didn't cover them.
- `export_all.py` takes `-- --no-render` to remake the masks from the renders already there.

**Changed in review (2026-11-17)**, from the reviews of the pull request that adds these files:
- A bare mark was cut out of the shield-sized render, only about 90 to 125 px across, and blown up to 256 px. Now the
  render script closes the camera in on each bare mark, so it fills the 256 px frame like the shield does, and the
  cropping in `export_all.py` is gone.
- Each mask was shaded against its own picture's middle brightness, so the same oak came out a little lighter or
  darker from mark to mark and from the small drawing to the big one. Now every mark is shaded against the plain
  shield (the small marks against the small plain shield), the clasps against one corner, the band against itself.
- The masks are named by where they show: `<piece>@<size>.png` and `<piece>@<size>@2x.png`, size in
  device-independent pixels. A mark shown below 48 uses the small drawing at 1x and 2x, from 48 up the big one, so
  moving the window to a sharper screen never swaps the drawing. The old names were pixel sizes, and 48 px was both
  the small drawing's 24 at 2x and the big drawing's 48 at 1x.
- The command now has `--python-exit-code 1`, so a failed export ends with an error.

## Pass 10: The small marks (the Roundel on the Mods tiles)
The Mods tab's three tiles (*Add-ins*, *Override mods*, *Runs in the game*) each show *Ready* or *Needs a look* with a
mark at 32 px. They're still the Pass 0 Roundel with its tick and !, the last old pieces in the window.

- **Same word, same picture.** *Ready* is the lit lantern and *Needs a look* is the nail !, the same marks as the Home
  card's headlines. Someone who learned them on Home reads the tiles without learning anything new.
- **The Roundel catches up with Passes 2 to 7 in one go**, because the Shield already made the choices: a raised ring, a
  flange with the smith's rose-head rivets, a slight dome, hammer dents, and one knock on the rim.
- **Field and charge must contrast, so pick one of two fields** (rendered side by side):
  - *A. All iron*: a pale, polished boss under the blackened marks. Quietest, but iron on iron may not separate.
  - *B. A small targe*: a round oak disc in an iron rim, the Shield's family made round, which ties the tiles to Home.
- **Only the small and bare drawings.** Tiles show at 32 px, below 48, so the Roundel never needs the big drawing.
  Korcari gets the bare marks flat, as on Home.
- **Tests**: at 24 and 32 px the two read apart by shape alone. *Squint* picks the field. *Swap*: the two marks moved
  from the Shield to the Roundel still belong. `export_all.py` gains the Roundel at 32 (and 64 at 2x).

**Done (2026-11-22)**, picked **A, polished iron**. Sheet in `renders/pass10-sheet.png` (rows: A lantern, A !,
B lantern, B !, in gold and lavender, at 64, 32 and 24 px, then Korcari's flat marks), close-up of both fields in
`renders/pass10-fields-close.png`.
- **The boss** (*Roundel boss (iron)*) is a profile line turned by a *Screw* modifier: a slight dome, a raised ring and
  a flange. Move the line's points in Edit Mode and the whole boss follows. Its material, *Polished iron grey (the
  roundel)*, is the smith's hammered iron made lighter and smoother, with shallower dents.
- Six of the shield's rose-head nails go round the flange, and *Knock in the rim (cutter, move me)* dents the ring
  at the lower right.
- The Roundel is now 1.8 across (it was 1.6), the Shield's height, so both fill the frame alike.
- **The marks are linked copies** of the Shield's lantern and nail !: reshape one on the Shield and the Roundel's copy
  follows. Each hangs from an empty (*Lantern on the roundel (move, scale me)*, *Nail on the roundel (move, scale
  me)*), at 1.3 times the Shield's size and centred by its whole shape.
- The targe (B) is kept in *Roundel body: other (oak targe)*, and the Pass 0 disc in *Roundel body: old (blockout)*.
  The render script only draws a body named *small* or *big*, so renaming swaps a field in. The old tick and ! are
  switched off.
- Tests: *Squint* passes at 24 and 32 px for both fields; A is the calmer next to text, B's grain turns to stripes
  behind the mark. *Swap*: the Shield's marks sit on the Roundel unchanged.
- `export_all.py` writes `mark-roundel-ready-lantern` and `mark-roundel-needs-a-look-nail` at 32 (and `@32@2x`), with
  their `-bare` versions; the mock's Mods tiles use them.

## Pass 11: The clasp and band join the set
The clasp and the band were made before Passes 3 to 7, so they share the light (Pass 8) but not the hand. Next to the
finished marks they can look like they came from another shop.

- **The smith's iron is shared** (Pass 6): the clasp's rivets become the same rose heads, and its strap gets the
  shield's hammered iron with its dents and polish.
- **Wood that touches the shield's wood varies**: the clasp's oak moves to the Shield's hewn oak family, keeping its own
  grain angle.
- **Time and use** (Pass 7) where hands would touch: the band's leather is darker and smoother near the middle, its
  stitching a little uneven, and one stitch is mended.
- **One echo each, at most**: the clasp's spear tip already echoes the shield's straps, so the band borrows nothing.
- **Still quieter than the words.** The test is the mock in all four looks: the clasps and band must look made by the
  same hands as the marks, and still recede behind the headline.

**Done (2026-11-22)**, sheet in `renders/pass11-sheet.png` (before on top, after below, in gold and lavender: the band
at 160 and 80 px tall, the clasp at 68 and 34).
- **The clasp's iron is the smith's.** Its rim uses *Hammered iron grey (the smith's, from marks.blend)*, a copy of the
  shield's iron with the grip rub turned off (nobody grips a clasp). Its three rivets are now the shield's rose heads
  (*Rose head nail (from marks.blend)*): a big one on the corner, which carries the clasp at 34 px, and a smaller one
  on each arm. The old ball rivets wait in *Clasp: old rivets (off)*.
- **Your mitred oak keeps its grain** and gains the shield's adze scoops (Pass 5), laid along each board's own grain.
  The nodes are labelled *(Pass 11)*; *Adze depth* sets how deep.
- **The band's leather** is darker and smoother along the middle, a little patchy, and its grain is rubbed flat there
  (*Worn along the middle (how wide)*, *Darker where worn*, *Smoother where worn*). The first try was twice as dark
  and read as smudges between the lozenges, so it was halved. The wear depends only on the distance across the strip
  and on the leather's own 4-unit circle, so the band still tiles: its two edges differ no more than neighbouring
  columns, as before.
- **The stitches** are a little uneven (*A little uneven (Pass 11)*, a Displace after the Array), and one is mended
  with a cross-stitch in newer, lighter thread (*Mended stitch (move me)*). The mend repeats once per tile; at 80 px it
  is a detail you find, not one you see.
- These copies of the smith's iron and nails don't follow changes made in marks.blend. A shared `materials.blend`
  (Pass 0's suggestion) would make them one; it isn't built yet.
- Backups, on the owner's machine only (`.gitignore` keeps `*.before-*.blend` out of the repository):
  `clasp.before-pass11.blend`, `band.before-pass11.blend`.

## Pass 12: One maker's materials
Passes 10 and 11 copied the smith's iron and rose heads into clasp.blend, so a change in marks.blend no longer reaches
the clasp. Pass 0 suggested the fix: one file of shared materials that every piece links, the way they link the light
from `rig.blend`.

- **`materials.blend`** holds the shared recipes as node groups with inputs
  ([ADR-0053](../../docs/adr/0053-one-makers-materials.md)): *Smith's iron* (the hammered, blackened and polished
  irons are one recipe with different values), *Hewn oak* (the grain, fibres and adze scoops every oak shares),
  *Tooled leather*, *Linen* for the Banner, and the rose-head nail.
- **Each piece links them** and keeps thin materials of its own: the group plus this piece's values, and around it
  whatever only this piece has (the shield's old paint, the clasp's cracks and mitre seam). A surface only one piece
  uses, like the walnut, the stone or the wax, stays where it is.
- **Proof it changed nothing**: every render before and after matches to within render noise, as Pass 8 proved for
  the light.
- ADR-0052 §2 names only the light as shared, so ADR-0053 comes first.

## Pass 13: Say it by kind (crests for a mod with no picture)
ADR-0050 §3: a mod with no picture shows a crest in the theme's colours, by its kind. It shows in a Mods row (about
40 px), on a card, and on the mod's *About this mod* page (about 160 px), so it needs the small and big drawings.

- **The body is the Banner**, the third starter, so a crest never looks like a status: the Shield says how the game
  is, the Roundel says how a kind of mod is, the Banner says what a mod *is*.
- **One charge per kind**, picked like Pass 1 from a sheet at 24, 40 and 64 px. Suggestions to start from:

  | Kind | Suggested charge | Why it reads |
  |---|---|---|
  | Add-in | **A key**, or a scroll with a ribbon | It opens something new in the game |
  | Override mod | **A hammer**, or a patch stitched over cloth | It reworks what's already there |
  | Runs in the game | **A lit brazier**, or a gear-like wheel | It runs while you play (but *light means good* belongs to the headlines, so it may stay unlit) |

- **The tests**: the three read apart by shape alone at 24 px; none can be mistaken for a headline mark or a tile
  mark; and *Swap*: on the Shield they'd still look like the set.

**Picked (2026-11-22)**: **key** for an add-in, **hammer** for an override mod, **wheel** for a mod that runs in the
game. Sheet in `renders/pass13-sheet.png` (each at 64, 40 and 24 px in gold, 40 and 24 in lavender, then all six at
24 px beside the headline and tile marks), close-up in `renders/pass13-close.png` (key, hammer, brazier; scroll,
patch, wheel).
- The six are flat outlines in *Banner: add-in (key)*, *(scroll)*, *Banner: override (hammer)*, *(patch)*, *Banner:
  runs in the game (brazier)*, *(wheel)*, in the marks' blackened iron for now (Pass 14 stitches them in). Each part is
  its own piece: two shapes in one piece that overlap cut a hole, which is what first put a slot down the key's shaft.
- **What read**: the hammer is a bold T at every size. The key is a keyhole shape at 24 px with its ring on top, so it
  stays apart from the headline !, whose dot is at the bottom. The wheel is clear from 40 px and a spoked disc at 24.
- **What didn't**: the scroll and the patch both melt into a rectangle at 24 px, and the brazier reads as a stool.
- **Not mistaken for a status**: in the squint strip the Banner's swallowtail keeps every crest apart from the
  Shield's and the Roundel's marks. The wheel stays unlit, so light still means good only on the headlines.
- The mock's vector crest drew *runs in the game* as a lit lantern, the *Ready* mark; it now draws the wheel. The
  mock's crests stay vector shields with their field divided by the mod's name until Pass 14 gives the Banner its
  drawings.
- The old tick and ! on the Banner are switched off. Backup, on the owner's machine only: `marks.before-pass13.blend`.

## Pass 14: The Banner catches up
Passes 2 to 7 for the Banner in one go, as Pass 10 did for the Roundel, now on the shared materials.

- **Built**: real folds (a few shape keys you can push around), a crossbar with turned ends, cords.
- **Surface**: linen with a visible weave on the big drawing, plain on the small; the charge stitched in, not nailed.
- **Age**: a darned patch (waiting since Pass 4), frayed hem threads, the cloth a little sun-faded at the folds.
- **Made by hand**: uneven stitching round the charge, like the band's.

## Pass 15: In the window, all together
Pass 9 again, with everything: the headline marks, the tile marks, the clasps, the band and the crests in the mock, in
all four looks, at real sizes. The test is unchanged: nobody looks at an ornament before the words, and every piece
looks made by the same hands.

## Pass 16: Motion (things that move when something happens)
A test is already in marks.blend: the Banner sways from its bar while folds run down the cloth (*Banner wind (lattice,
Wind 0 = still)*). One property, *Wind*, drives it all: at 0 the motion modifiers are switched off in the render, so
every still is the same pixels as before (checked against Pass 13); at 1 it plays a 4-second loop whose last frame
flows into its first. Preview: the private *Banner motion test* page.

- **Motion says something changed, then rests.** The door swings open on your first visit; the lantern's wick
  catches when the game becomes ready; the nail ! is struck in once when something needs a look; the signpost's board
  swings and settles; a crest's banner lifts once when you turn its mod on. Each plays once, in about a second.
- **Loops only for what is alive by nature**, and slow: a flame's flicker, a banner in a breeze while you point at it.
  Never more than one loop on screen at a time, and nothing in a long list loops.
- **Stills always exist.** Windows' *Animation effects* off, the window in the background, or Korcari: every piece shows
  its still.
- **Drawn the same way as the stills**: Blender renders the frames, `export_all.py` turns them into one strip of grey
  masks per piece and size, and the app tints them exactly as it tints a still. A glow that only brightens can be the
  app fading one still over another instead.
- The app side (when each piece plays, the setting, the strips' budget) is drafted as
  [ADR-0054](../../docs/adr/0054-motion.md) (Proposed), and needs a place on the roadmap before any App code.

## Realism passes (Passes 17 to 22): the medieval look, made believable
Passes 2 to 7 made the pieces look made. These make them look *real*: things a smith, a joiner or a chandler of the
time would recognise. The guide is how such things were actually made, which also keeps them our own: real history,
not anybody's game.

Two rules hold for every one of them:
- **Realism lives in the big drawing and the motion.** The small drawing stays plain (ADR-0052 §4), and every pass
  ends with the 24 px check: if a mark reads worse, the detail goes.
- **Still grey, still calm.** Realism comes from shape, roughness, relief and light, never from colour, and never
  louder than the words.

## Pass 17: Made the way it was made (period construction)
Each piece rebuilt to how it would really have been made. Suggestions:

| Piece | How it was really made |
|---|---|
| Shield | Glued oak boards faced with linen or rawhide under gesso, the edge bound in rawhide or iron, nails clenched (bent over) on the back |
| Lantern | A *lanthorn*: panes of thin horn, not glass, so the light comes through soft and milky; a pierced tin or iron top for the smoke |
| Candle | On a pricket (an iron spike) rather than in a socket; the wax uneven where it was dipped |
| Door | Planks on ledges, strap hinges ending in curls, studded with clench nails, under a rounded arch |
| Key | A warded key: a bit cut to pass the wards, a hollow shank, a bow forged round and a little out of true |
| Hammer | A smith's cross-peen, the face crowned and polished by use, the handle wedged through the eye |
| Banner | Wool or linen, the charge appliquéd and edged in couched cord, a sleeve over a crossbar with turned finials, cords and tassels |

## Pass 18: Close up (surfaces at the scale they really have)
- **Oak** shows its rays: the silver flecks of quarter-sawn oak, and open pores along the grain.
- **Hand-forged iron** has fire scale (a dark skin, flaking at edges), hammer facets, and a bright polish only where
  it's handled.
- **Linen** has a weave with slubs (thicker threads at random), **leather** its pores and creases, **wax** a soft
  sheen and drips that thin as they run.
- **Roughness from measured ranges**, not guesses: polished iron smooth, fire scale and linen matte, wax in between.

## Pass 19: Light that behaves (contact, bounce, glow)
- **Contact shadows** where every charge meets its field and every nail meets its wood, so nothing looks pasted on.
- **The lit pieces light their surroundings**: the lantern and candle cast a warm pool on the shield around them, and
  the doorway's light spills out onto its sill.
- **A little bounce** from the field into the undersides of the charge, so dark iron still shows its shape.
- Still one sun from `rig.blend`; this adds what real light does around it, not a second light.

## Pass 20: Lived in (history you can read)
- **Soot** above the lantern's chimney and the candle's flame, faint on the shield behind them.
- **Wax** dripped on the candle's dish and once, long ago, on the shield.
- **Wood that moved**: a board slightly cupped, a hairline gap where two boards shrank apart.
- **Nothing quite symmetric**: rivets of a few sizes, a hinge strap a touch longer on one side, a key bow out of round.
- **Repairs** that tell a story: a newer board, a replaced nail (Pass 7 has one), a patched banner (Pass 14).

## Pass 21: Motion with weight (real physics)
Each clip in ADR-0054 moves the way the real thing would.
- **The lantern swings like a pendulum of its size** (a longer hang swings slower), and its glow and shadow swing with
  it.
- **A flame flickers in two layers**: a quick small flutter over a slow lean, never a regular blink.
- **The banner is a cloth simulation** with wind, baked and blended into a seamless loop, so its folds fall and catch
  like real cloth (the test's lattice sway becomes its fallback).
- **The doors open with weight**: a slow start, a swing, and a small settle against the hinges.
- **The nail is struck once**: a sharp drop, a tiny bounce, a puff of dust that fades.
- **Secondary motion**: a key's ring, a banner's tassels and cords follow a beat behind.

## Pass 22: Atmosphere (for the bigger set pieces)
Only where a whole scene is drawn (the look's backdrop, the setup hall, *Play*), never behind a mark.
- **Volumetric light**: a shaft of light with dust drifting through it, rendered as a grey mask like everything else.
- **Mist, embers and wisps** for the looks' backdrops (Witch of the Wilds, Blood and Shadow, The Fade), slow, seamless
  loops at the edge of the window.
- **Depth**: near things sharper, far things softer and lighter, as air does.

## Ideas for 1.0 (not planned yet)
Ways to make modding feel like part of the game, each tied to a moment in the app. All our own art, all calm.

- **The setup hall**: on the first run, a wall of unlit lanterns; each lights as a check passes (game found, documents
  found, backups ready). Progress you can see without reading.
- **Sealed**: while changes are applied, wax warms; when they're done, a seal is stamped (*Sealed*). Undo cracks it.
- **Banners in a row**: add-ins' priority shown as banners hung on a rail, reordered by rehanging them.
- **Where mods overlap**: two crossed banners, the one that wins in front.
- **Empty places with a picture**: an empty rack (*Nothing here yet*), two banners hanging side by side (*Nothing
  overlaps*).
- **A backdrop for each look** behind the header: our own painted silhouettes with slow drift (mist at a forest edge
  for Witch of the Wilds, embers for Blood and Shadow, wisps for The Fade; none for Korcari).
- **Play**: the doors swing open across the window, then the game starts.
- **Your crest** (below), with the headline marks hanging on it, and perhaps a small familiar of our own making that
  speaks the cozy guide's lines (ADR-0051).
- **Sound, off unless you turn it on**: a clink when a mod turns on, a page turning, a wick catching.


## Your crest (an idea for later)
Every profile could have a crest of its own in a corner of the window, next to the profile's name: a small companion
that's yours to make.

- **It's built from parts**: a shield shape (heater, round, kite, banner), a division (per pale, per bend, quarterly,
  chevron and so on), a charge (lantern, key, hammer, oak leaf, our own drawings), and a motto line you type. Colours
  come from the theme's tokens, so every crest reads in every theme.
- **It's yours from the first second**: a crest is made from the profile's name the moment a profile exists (the same
  way the mock gives a mod without a picture a crest), with *Make it mine* to change any part, or *Try another*.
- **It keeps you company**: the readiness marks from Pass 1 hang *on your crest* instead of on a stranger's shield. The
  lantern lights when the game is ready, a ! tag hangs on it when something needs a look, and the door opens on your
  first visit. This turns the shield you found bland into your own shield.
- **It stays calm**: no points, no streaks, nothing that nags. Maybe one quiet keepsake (a sprig of laurel after the
  first launch that works), and only if that feels right to you.

This is a new feature, so it needs an ADR (it extends ADR-0017's look per profile, and ADR-0026's art rules) and a
place on the roadmap before any app code. The art side is a parts kit: four shield shapes, about eight charges and the
state badges from Blender, and the divisions as simple vector shapes.
