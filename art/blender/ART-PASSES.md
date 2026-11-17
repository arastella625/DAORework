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
