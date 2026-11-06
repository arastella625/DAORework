# ADR-0037: A mod is one thing, and turning it on takes over what is in the game

- **Status:** Accepted
- **Date:** 2026-11-05

## Context
The window has two verbs for a mod that is both in the Library and in the game: *Turn on* and *Manage*. Their order
matters, and nothing says so. On the owner's first real run, *Turn on* came first, which copied 7.3 GB beside the same
files Vortex had left, and *Manage* then refused those mods. The owner asked why a mod can be turned on at all if it
should be managed first ([WORKFLOWS.md](../WORKFLOWS.md)).

ADR-0012 says a file DAORework did not place is never touched. ADR-0015's exception is an adoption the player confirmed.
P2.6-20 already finds the override files that are the same as a mod's.

## Decision
- **Turning a mod on takes over identical files already in the game.** When a mod the change adds has an override file
  and the override folder holds a file DAORework did not place with the same name and content, the plan moves that file
  to the mod's target instead of copying from the library. Any further identical copies are deleted, with a backup.
- **This is an adoption the player confirms.** It is in the preview, backed up, journaled and undone like any change, so
  ADR-0015's exception covers it. Files that differ are not touched and still warn.
- **Not while Vortex can put them back.** A mod whose files a live Vortex record lists is left as it is today, since
  Vortex would restore what moved (ADR-0028 §2). A left-behind record doesn't stop it.
- **Only mods the change adds.** Changing the order or turning a mod off takes nothing over.

## Consequences
- The duplicate can't be made, and turning on a mod already in the game costs a rename, not a copy.
- Mods that are not exact matches for a Library entry are covered too, since it works file by file.
- A preview can say *moves*, where a player expected *copies*.
- A moved file may also be used by a mod that isn't managed, so turning the first mod off later takes it from both. The
  preview says so when a moved file sits beside other files. The Library keeps the file, and *Undo* puts it back.
- *Manage* stays for files with no Library copy, until the lists merge (P2.6-26).

## Alternatives considered
- **Hide *Turn on* until a mod is managed.** Keeps two verbs, and adds a rule to learn.
- **Delete the duplicates after copying.** Copies and backs up gigabytes to end up in the same place.
- **Send matching mods through *Manage*.** Only covers exact matches: 43 of 69 on the owner's library.
