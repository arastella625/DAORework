# ADR-0042: Finding the problem mod

- **Status:** Accepted
- **Date:** 2026-11-06

## Context
A crash or a broken quest doesn't say which of 137 mods caused it. A less technical player's only way to find out is to turn
mods off by hand, and most stop there. Halving the list each time needs about 8 rounds, but it is a lot to keep track of by
hand, and the rounds take days of playing, with the app closed in between.

## Decision
1. **Halve what could be the cause.** The suspects start as every mod that is on. Each round turns off half of them. All
   other mods stay on, in their own order, so a mod that needs another still has it. If the problem still happens, the
   half that was off is cleared; if it's gone, the cause is in that half. This repeats until one is left.
2. **The player answers, DAORework never guesses.** It can't see the game, so *It still happens* and *It's gone* are the
   only input.
3. **Every round is an ordinary change.** Each is a plan shown as a preview, applied through the same journal and backup,
   with *Undo* in History like any other. Nothing moves without *Apply*.
4. **One round of proof.** If the problem happened in every round, nothing has shown that the last mod is to blame, so
   one more round turns off just that mod. If the problem still happens, the answer is *It doesn't look like one of your
   mods*, not a false name.
5. **Everything goes back.** The end offers *Put everything back without* that mod, or *Put everything back*, and the
   player can stop at any round. The original list is kept, so the order is the player's own again.
6. **It survives closing the app.** The search is kept per documents folder in `problem-searches\{documentsKey}.json`, a
   versioned document. It is saved once a round has been applied, so a cancelled preview leaves the round as it was.
7. **Warn before every round.** A save made with mods off can break for good, so each round says *Don't save while testing*,
   and to quit without saving or use a throwaway save.
8. **The player can narrow it.** The search keeps the player's own words for the problem and shows them each round. *Keep
   my looks on* leaves mods that only change looks out of it, which cuts the rounds, so it can't find a mod among them.
   *Check with no mods first* turns everything off once, to learn whether it's a mod at all: if the problem still
   happens, the search ends there.
9. ***Forget this search* changes nothing.** It is the way out if the mods have changed so much that going back can't
   be planned, such as a mod removed from the Library meanwhile.

## Consequences
- Finding one mod among 137 takes 8 rounds, or 9 if every answer is *It still happens*.
- It finds one mod. Two mods that only clash together may end on one of them, or on *It doesn't look like one of your
  mods*, and the window says so.
- A save made with a mod off may not load the same with it on, which is why every round warns.
- Each round goes by the search's own list, so a mod turned on during a search goes off again in the next round. The
  preview shows it first, and *Forget this search* keeps the mods as they are.
- *Start* sits with *Combined files*, and Home and the Mods tab each have a *Find the problem mod* button that opens it
  (P2.6-63). A search under way shows near the top of the Mods tab, whichever chip is on.

## Alternatives considered
- **Turn off one mod at a time.** Simple, and up to 137 rounds of playing.
- **Leave cleared mods off to save moves.** Fewer files move each round, but a mod that needs a cleared one would break
  and mislead the answer.
- **Keep the search only in memory.** A round means playing the game, often after closing DAORework.
