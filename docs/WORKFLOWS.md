# How a player moves through DAORework

**Status:** a first draft for the owner to correct, written 2026-11-05 from the code and the owner's first real run, and
updated after the work it led to. It says what a player should be able to do without being taught, and where the window
still fails that. "Now" is from tests and rendered screens with made-up mods, not a real library.

**The test for every screen:** a player who has read nothing can see what to do next, and no pair of buttons lets them
do things in the wrong order. A note saying "do this first" means the design failed. Change the design instead.

## One idea: a mod is one thing

A mod is the same mod whether it is in the Library, in the game, or both. Players think "I have this hair mod", not "I
have a library entry and an unmanaged folder". Today the window has two lists and two verbs for that one mod, *Turn on*
and *Manage*, and the order between them matters. It shouldn't. See [ADR-0037](adr/0037-a-mod-is-one-thing.md).

## The journeys

### 1. New player, no mods yet
1. Open DAORework. It finds the game and its documents folder.
2. *Add mods*.
3. *Turn on*, preview, *Apply*.
4. *Play*.

**Now:** the next-step card and first steps lead from *Add your first mod* to *Turn on*, and go once a mod is on. Still
wrong: a profile comes first, in setup words, and *Check the game* competes with the card for the first move.

### 2. Coming from Vortex
1. DAORework says it found mods Vortex placed, and how many.
2. If Vortex can still reach them: copy, purge in Vortex, turn on. Vortex would put back anything moved, so this order is
   forced (ADR-0028).
3. If it can't (left behind): take them over in one change.
4. Choose who wins where mods really differ.
5. *Play*.

**Now:** the next step says how many mods are in the game and *Take them over* opens one preview, with *Who wins* and
*Swap*. *Turn on* moves identical files, so the owner's two copies can't be made again, and *Manage* clears them where
they already exist. Still wrong: *Who wins* covers mods, not several versions inside one mod (*DA2 Style 2-Handed
Weapons*), and add-ins Vortex placed aren't covered (#116).

### 3. Mods placed by hand
1. DAORework lists what it found.
2. Take them over, in one change.
3. From then on they are ordinary mods.

**Now:** the same path as journey 2. A mod in the Library and the game shows once. Still wrong: *Found in your game*
keeps the old long rows and the word *Manage*, and the two panels remain.

### 4. Adding one new mod
1. Download it. DAORework notices, if the player asked it to, or they use *Add mods*.
2. *Turn on*, preview, *Apply*.
3. If it clashes, the preview says who wins, and *Arrange* changes that.

**Now:** it works, with search, one-line rows, drag and a preview that stays in view. It is the journey to protect.

### 5. Something has gone wrong
1. The game won't start, or a mod doesn't show.
2. *Check the game* says what is wrong and what to do.
3. *Undo* in History goes back.

**Now:** the next step leads to an unfinished change, for mods and fixes. A running change says so and locks the window.
Still wrong: *Check the game* isn't part of the next step, so a game that won't start doesn't lead anywhere by itself.

## What this asks of the window

- **Turning a mod on takes over what is already in the game.** Done (P2.6-25).
- **One list, one row per mod,** wherever it is. In part (P2.6-26): copies of a mod are still separate rows, until a copy
  can be removed.
- **One next step,** worked out from where the player is, in the same place every time. Done (P2.6-27).
- **Words that say what happens.** Not done: *Manage* is the first to question.

## Questions for the owner

1. Are these the right five journeys? Which one is missing?
2. Should the first screen ask for a profile at all, when the game can be found without one?
3. Should a player coming from Vortex be offered everything at once, or a few mods first?
