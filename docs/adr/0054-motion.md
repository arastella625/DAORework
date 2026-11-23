# ADR-0054: Motion: what moves, when, and how it's drawn

- **Status:** Proposed
- **Date:** 2026-11-22
- **Amends:** [ADR-0052](0052-our-own-art.md) §2 (the export also writes motion) and §4 (a moving piece's rest frame
  is its still); builds on [ADR-0050](0050-a-calmer-window.md) §1 (the headline card) and §3 (the crest)

## Context
For 1.0 the owner wants the window to feel like part of the game: full functionality, and the art and liveliness
players love in modern games. "Modding can be boring, but not here." The art is ready for it. ADR-0052's pieces are
modelled in Blender, and `marks.blend` already has a working test: the Banner sways from its bar while folds run down
the cloth, a 4-second loop of 48 frames, drawn as grey masks the app tints like every still (the private *Banner
motion test* page shows it in each look).

Today the only motion in the window is a success message fading (VOICE rule 4). No ADR says:
- which pieces may move, and when, without breaking ADR-0050's calm window;
- what happens for a player who turned animations off in Windows, or who finds motion tiring;
- how a moving piece is drawn so that every theme and custom look (ADR-0017) still recolours it;
- what it may cost in size, memory and battery.

## Decision
1. **Motion follows a change, then rests.** A piece moves because something just happened, plays once in at most
   1.5 seconds, and ends on its still:
   - *Welcome*: the doors swing open. *Ready to play*: the lantern's wick catches. *Not ready yet*: the nail ! is
     struck in. *One thing first*: the signpost's board swings and settles. *Ready to play, without your mods*: the
     candle is lit.
   - It plays when the headline changes, not each time Home is shown.
   - A tile's mark on Mods plays when its kind becomes ready or needs a look. A crest lifts once when its mod is turned
     on.
2. **A loop only for what's alive by nature**: a flame, cloth in a breeze.
   - At least 3 seconds a cycle, small, and seamless.
   - At most one loop on screen at a time. Nothing loops in a list, a grid of cards, or a sheet.
   - A loop plays only while the window is active and its tab is showing.
3. **Every piece has a still, and the still wins** when:
   - Windows' *Animation effects* is off (Settings › Accessibility › Visual effects; *Show animations in Windows* on
     Windows 10), read in Infrastructure behind a port;
   - the player chose *Motion: Off*;
   - the look is *Korcari*, which draws flat and still;
   - the window is in the background or minimised.
4. **One preference, *Motion*: *Follow Windows* (the default), *On* or *Off*.** It lives in Settings with the look,
   is kept in `preferences.json` with the other preferences, and is app-wide, not per profile.
5. **Drawn the same way as the stills** (amends ADR-0052 §2).
   - Blender renders the frames, at 12 a second. A piece's motion is driven by one property (*Wind* on the Banner),
     and at 0 the motion is switched off in the render, so the stills don't change.
   - `export_all.py` writes each clip as one strip of grey masks, frame after frame, named
     `<piece>@<size>-<clip>.png` and `@<size>@2x-<clip>.png`. Clips are named by what happens: `-arrive` (play once),
     `-idle` (loop).
   - The app tints every frame exactly as it tints a still (ADR-0052 §3), so every theme and custom look recolours
     the motion too.
   - A clip's last frame (for `-arrive`) or first frame (for `-idle`) matches the piece's still, so motion never jumps
     into or out of rest (amends ADR-0052 §4).
   - A change that only brightens or dims (a glow coming up) can be the app fading one still over another, with no
     strip.
6. **A budget.**
   - A clip has at most 48 frames. A strip at 2x is at most 400 KB; the Banner test is 360 KB.
   - A strip is decoded only while its piece is on screen and freed after.
7. **Motion never carries meaning alone.** The headline's words still say what changed; a player who sees no motion
   loses nothing but the motion.
8. **Sound is not part of this.** Any sound needs its own ADR.

## Consequences
- The headline card can greet a player, and the window reacts to what they do, while lists and the rest of the window
  stay as calm as ADR-0050 made them.
- Each piece gains clips to model and export. A clip is a Blender edit and one command, like a still, and
  `ART-PASSES.md` records each.
- Infrastructure gains a small port for Windows' animation setting, with a Linux implementation after 1.0.
- The App gains one control that plays a strip and falls back to the still. Headless tests cover it: *Motion: Off*,
  the Windows setting off, *Korcari* and an inactive window each show the still; a clip ends on its still.
- The mask test from ADR-0052 grows: every strip divides into frames of the size its name says, and a clip's rest
  frame matches its still within render noise.
- The repository grows by the clips the App draws. At the budget, the five headline clips and two loops (lantern and
  candle), each at 1x and 2x, come to about 3.5 MB.
- No App code is written until a roadmap task takes this. The bigger set pieces in `ART-PASSES.md` (a setup hall of
  lanterns, a wax seal when changes are applied, doors opening on *Play*) follow these rules, each with its own task.

## Alternatives considered
- **No motion:** calm, but it gives up what the owner wants 1.0 to be.
- **Video files (MP4, WebM):** Avalonia has no video player built in, and a video is one fixed colour, so every theme
  and custom look would need its own copy.
- **Animated GIF or APNG:** fixed colours again, and GIF has no partial transparency for the edges.
- **Vector runtimes (Lottie, Rive):** a new dependency for art that's modelled and rendered, not drawn as vectors.
- **Moving the still only (turning or skewing it in the app):** cheap, but a door can't open and cloth can't fold
  that way. Kept for a glow, as in 5.
- **Always-on idle loops everywhere:** lively for a minute, tiring after, against ADR-0050, and costly on battery.
- **Following only Windows' setting, with no preference of our own:** a player who likes Windows still but wants a
  lively DAORework, or the reverse, would have no say.
