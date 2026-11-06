# ADR-0043: What changed since the last game

- **Status:** Accepted
- **Date:** 2026-11-06

## Context
The first question when a game breaks is *what changed?* A player who added three mods last week rarely remembers which.
DAORework's History lists every change, but that is a list to read, not an answer, and it doesn't say which set of mods the
game was last run with.

DAORework can't see whether the game worked. A play only says the game was started with these mods.

## Decision
1. ***Play* records the exact mods.** Each launch from the window keeps the time and the ordered list of mods on, per
   documents folder, in `play-history\{documentsKey}.json`, a versioned document. The same list again only moves the
   time on. The last 10 different lists are kept.
2. **Compare with the last game that had different mods.** The latest play with the mods on now tells nothing, since it may
   be the broken one. So the comparison is with the most recent play whose list differs.
3. **Say it honestly.** The window says *If your game last worked Tuesday, 19:05, this is what's new since then*, then what
   was added, what was removed, and whether the order changed. It never says the game did work.
4. **Going back is an ordinary change.** *Go back to those mods* is a plan with a preview and *Undo*. A mod no longer in the
   Library is left out, and the preview says so.
5. **A search doesn't count.** While *Find the problem mod* is running (ADR-0042), plays aren't recorded, or its rounds
   would bury the list the player trusts.
6. **Only the window records.** A game started from the command line, Steam or a shortcut isn't seen. That can only make
   the comparison older, never wrong.

## Consequences
- A player gets a short answer to *what changed?* without reading History, and one button to try the old set.
- A failure to record never stops the game. A history that can't be read starts again, since it only helps.
- Fixes such as DXVK aren't part of the record yet, so a change to one doesn't show.

## Alternatives considered
- **Ask after each game whether it worked.** The only way to really know, but a question after every game is one nobody
  wants, and most would be skipped.
- **Compare with the latest play.** Often the broken one, so it would say nothing changed.
- **Work it out from History.** It holds changes, not plays, so it can't say which set was last run.
