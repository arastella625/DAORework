# How a player moves through DAORework

**Status:** a first draft for the owner to correct, written 2026-11-05 from the code and the owner's first real run. It
says what a player should be able to do without being taught, and where the window fails that today.

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

**Today:** Home opens on *Welcome* and asks for a profile first, then *Game folders*. The first step is setup words, not
the player's goal, and nothing leads from a finished check to *Add mods*.

### 2. Coming from Vortex
1. DAORework says it found mods Vortex placed, and how many.
2. If Vortex can still reach them: copy, purge in Vortex, turn on. Vortex would put back anything moved, so this order is
   forced (ADR-0028).
3. If it can't (left behind): take them over in one change.
4. Choose who wins where mods really differ.
5. *Play*.

**Today:** the *Move from Vortex* card shows only when Vortex is live. A left-behind player, like the owner, gets no card:
69 rows under *Found in your game*, each repeating the same paragraph, and nothing saying "take them all over". Nothing
stopped the owner choosing *Turn on* first, which left two copies of 77 mods.

### 3. Mods placed by hand
1. DAORework lists what it found.
2. Take them over, in one change.
3. From then on they are ordinary mods.

**Today:** the same two-verb problem as journey 2, and the word *Manage* doesn't say what it does.

### 4. Adding one new mod
1. Download it. DAORework notices, if the player asked it to, or they use *Add mods*.
2. *Turn on*, preview, *Apply*.
3. If it clashes, the preview says who wins, and *Arrange* changes that.

**Today:** this one mostly works. It is the journey to protect.

### 5. Something has gone wrong
1. The game won't start, or a mod doesn't show.
2. *Check the game* says what is wrong and what to do.
3. *Undo* in History goes back.

**Today:** a change left unfinished shows on *History*, which the player has to think of opening. Home doesn't lead there.

## What this asks of the window

- **Turning a mod on takes over what is already in the game.** A file that matches is moved, not copied. The duplicate
  can't happen.
- **One list, one row per mod,** wherever it is. Copies of a mod are one row.
- **One next step,** worked out from where the player is, in the same place every time.
- **Words that say what happens.** *Manage* is the first to question.

## Questions for the owner

1. Are these the right five journeys? Which one is missing?
2. Should the first screen ask for a profile at all, when the game can be found without one?
3. Should a player coming from Vortex be offered everything at once, or a few mods first?
