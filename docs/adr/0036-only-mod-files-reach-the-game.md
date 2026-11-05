# ADR-0036: Only mod files reach the game

- **Status:** Accepted
- **Date:** 2026-11-04

## Context
A library moved over from Vortex holds 983 `__folder_managed_by_vortex` markers and 6 Mac leftovers among its mods, and
most mods carry a `readme.txt`. DAORework copied all of them into `override` and `AddIns`, as [ADR-0012](0012-mod-storage-model.md)
says: the library keeps each mod as its download lays it out, and deployment maps every file. So one marker's winner
changing was a real file change, a plan to turn on three mods "deleted" a marker, and a folder of Mac leftovers in
`override` became a mod called `__MACOSX`.

The game finds override files by name and reads the kinds it knows ([ADR-0033](0033-mods-that-run-inside-the-game.md) §1).
Nothing known reads a marker, a `.DS_Store` or a readme. That last part comes from how the game's files work and from
mods in use, not from a test: a mod that needs a `.txt` file in `override` is not known to exist, but can't be ruled out.

## Decision
- `NonModFiles` is the one list of files that aren't part of a mod for the game: what tools leave behind (Vortex's marker,
  `.DS_Store`, `._` files, anything under `__MACOSX`, `Thumbs.db`, `desktop.ini`) and documents (`.txt`, `.md`, `.rtf`,
  `.pdf`, `.doc`, `.docx`, `.odt`, `.nfo`, `.htm`, `.html`, `.url`).
- `ModLayout` leaves them out, so they are never deployed, never planned and never compared. Nothing else needs to know.
- The library doesn't change. A mod keeps every file it came with, so a readme can still be read there, and a mod
  already in the library needs no migration.
- A mod already on loses these files from the game at its next change, with the usual hash check and backup. The preview
  says so in one line, and *Undo* puts them back.
- A folder or loose files in `override` holding nothing else aren't offered as a mod. Managing a mod leaves its readme
  where it is, since it is no longer part of what moves.
- Adding a name to the list is a change to this ADR. It is a list of what is known not to matter, never a guess.

## Consequences
- Turning on 60 mods copies about 1,000 fewer files, and plans stop changing over markers.
- A mod that really did need a `.txt` in `override` would lose it without a word. If one turns up, it is taken off the
  list by extension or by name.
- Images are left alone: a preview `.jpg` still deploys, since mods do ship textures in picture formats.
- Rolling back past this change restores the record of the time, and the next plan tidies again.

## Alternatives considered
- **Only stop warning, keep deploying:** the warnings go, but plans still copy and swap files nobody needs, and
  `__MACOSX` stays a mod.
- **A list of what the game reads, deploying nothing else:** safer against clutter, but a file kind missing from the list
  would silently break a mod. The game's full list isn't known, so the risk is the wrong way round.
- **Removing the files at import:** the library would no longer be what was downloaded, the readme would be gone, and
  mods already in the library would need a migration.
- **A setting:** nobody can answer it. The player would need to know what the file does, which is the question.
