# ADR-0051: The cozy guide

- **Status:** Proposed
- **Date:** 2026-11-15

## Context
P2.9-03 asks for a guide to a first game with mods, the owner's *cozy guide*, with an ADR first: what it shows, when
it comes back, and its words. On 2026-11-15 the owner moved it into 0.8.0 with Phase 2.10, so it lands with the calmer
Home of [ADR-0050](0050-a-calmer-window.md).

Today P2.6-36's first steps (*Find your game*, *Add mods*, *Turn them on*) are three ticks on the next-step card, gone
once a mod is on. They say what to do, not why, and nothing brings them back.

## Decision
1. **Where:** the guide is Home's one card while it runs, not an overlay pointing at controls. Each step has a short
   heading, two sentences at most, a drawing, and one button that goes where the step is done. The window stays usable
   around it, by mouse and keyboard.
2. **The steps:**
   1. *Find your game*: which copy DAORework found, and *Game folders* if it's wrong.
   2. *Add mods*: where mods come from, and that adding one doesn't change the game yet.
   3. *Turn them on*: that each change shows a preview first and can be undone.
   4. *Play*: the *Play* button, and *Find the problem mod* for when something goes wrong.

   A step the player has already done shows as done, so a player who added mods before opening the guide starts at
   *Turn them on*.
3. **When it shows:** on the first start, and whenever no mod is on and the player hasn't closed it. *Skip the guide*
   closes it for good; Settings › *Show the guide again* brings it back at the first step not done. Finishing it says
   *You're set. Every change can be undone in History* once, and Home goes back to its usual card.
4. **Drawings:** made for DAORework, as vector drawings in the theme's colours, so every theme and look recolours them.
   No pictures from the game, from mods or from a pack whose licence forbids it ([ADR-0026](0026-workflow-first-layout.md) §5).
5. **Words:** [VOICE.md](../VOICE.md) gains the guide's words. It never says *simply* or *just*, and never blames.
6. **Remembered:** whether the guide was skipped or finished is one preference, kept with the others, not per profile.
7. **Offline**, like the rest of the window. Nothing it shows comes from the internet.

## Consequences
- P2.6-36's three ticks become the guide's first three steps, and the code that builds them is reused.
- The guide is one more thing to keep true when a step's screen changes. Its screenshot test catches the visible part.
- Keyboard and screen-reader use is designed in here, ahead of P2.9-04's audit, which then checks it.

## Alternatives considered
- *A tour that points at controls one by one:* hides the window, breaks when the layout changes, and is hard to use by
  keyboard and screen reader.
- *A first-start wizard before the window:* stops the player from looking around, and can't be done halfway.
- *A help page on the website:* the player has to leave the app, and it can't show which steps are done.
