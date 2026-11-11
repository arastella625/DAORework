# ADR-0046: The game reads one documents folder, and Home says when a profile uses another

- **Status:** Accepted (owner, 2026-11-10)
- **Date:** 2026-11-10

## Context
Dragon Age: Origins reads its saves, settings and mods from `BioWare\Dragon Age` inside the Documents folder Windows
reports, and nowhere else. ADR-0032 lets a profile name any folder, and finds one by searching the drives when the usual
places have none. That helps a player who wants DAORework to read a folder, but the game never follows: a profile can
name a folder the game doesn't read.

On 2026-11-10 the owner's setup turned out to be broken this way. Their game folders were copied to `D:\` on 2026-06-18,
while Windows' Documents stayed on `C:`. Their profile, made on 2026-10-31, points at `D:\BioWare\Dragon Age`, where
their mods and saves are. The game kept reading the one on `C:`, and made a fresh empty one there on 2026-11-04. The
game's own log names the folder it reads. Every mod DAORework turned on went to a folder the game never opens, and
nothing in DAORework said so. The existing documents checks look only at whether the profile's folder exists, or
whether another exists when it doesn't.

[VOICE.md](../VOICE.md) keeps folder paths behind *Details*, and names the folders only in *Game folders*.

## Decision
- **The game's folder is Windows' Documents known folder plus `BioWare\Dragon Age`.** `GameDocumentsFolderRule`
  compares it with the profile's documents folder. It never guesses a drive or a path, and doesn't read the game's
  log, which lives inside the folder being checked.
- **A link is the folder it points at.** Before comparing, a junction or symbolic link anywhere along either path is
  followed to the folder it finally reaches, through `IFileSystem`. The owner's fix, the game's path made a junction to
  `D:\BioWare\Dragon Age`, then reads as one folder and the warning goes. A link that can't be read compares as
  written, which can only show the warning, never hide it.
- **It warns only when both are true:** the profile names a different folder, and the game's folder exists. A game
  that has never been started has no folder yet, and the profile's folder may be the one it will make. On Linux the
  game reads its Proton prefix (ADR-0031), so the rule says nothing there.
- **A warning, and report only** (`DAO_DOCUMENTS_NOT_THE_GAMES`). The game still starts, so it isn't an error, but
  nothing DAORework does with mods reaches it, so it's the first thing *Worth a look* on Home shows.
- **Home names both folders.** This is an exception to VOICE.md's rule 2: here the folders are the diagnosis, and the
  player has to recognise which one holds their saves and mods to choose. The button opens *Game folders*.
- **Nothing is moved or linked automatically.** The step suggests choosing the game's folder in *Game folders*, or
  moving the saves and mods into it. The command line's advice also names a link from the game's path to the player's
  folder. Which folder holds what the player wants is theirs to judge.

## Consequences
- A player in the owner's place is told on Home, with the two folders and what to do.
- A player who keeps a copy of their folder elsewhere on purpose, and points a profile at it, sees the warning too.
  That's right: the game doesn't read that copy.
- Choosing the game's folder makes the profile's mods appear not to be on until the player moves them there, or turns
  them on again. DAORework's record is kept per documents folder (ADR-0012), so nothing is lost by switching.
- A new rule, a code and a next step on Home. The drive search of ADR-0032 stays: it still finds a folder for a game
  that has never written to Windows' Documents.

## Alternatives considered
- **Move the folder or make a link for the player.** Moves saves the player may care more about than mods, and a link
  across drives is easy to break. The task is to say what's wrong, not to decide which copy is right.
- **Read the folder from the game's log.** The log is inside the documents folder, so it can't find a folder the
  profile doesn't name, and it's written only once the game has run.
- **Keep the paths behind *Details*.** The player couldn't tell from *Your documents folder isn't the game's* which
  of their two folders holds their saves.
- **Stop ADR-0032 from naming a folder the game doesn't read.** A profile with no folder the game reads would have
  nothing to manage before the first launch, and the owner's profile already exists.
