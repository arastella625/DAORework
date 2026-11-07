# ADR-0045: Backups reuse the Library

- **Status:** Proposed
- **Date:** 2026-11-06

## Context
Every change copies every file it replaces or deletes into a backup, and nothing ever removes a backup. On the owner's
library the backups had reached 22.6 GB. One round of *Find the problem mod* (ADR-0042) added 7.4 GB, and a full search
is several rounds, so a tool meant to help a player could fill their drive.

Nearly all of that is waste. Those files came from the Library, which still has them, and a library file never changes
after import (`IModLibrary`). ADR-0015 already restores a moved file from the Library by its hash.

The owner chose this over a button that clears old backups (2026-11-06), which would leave the work to the player and lose
*Undo* for whatever was cleared.

## Decision

### What isn't copied
A backup leaves a file to the Library when all of these hold, and otherwise copies it as before:
- the record from before the change names that path with that hash, so DAORework placed it;
- the file on disk still has that hash;
- a file in the Library has that hash, and reading it proves it.

The entry keeps the file's hash and has no `StoredAs`. Going by the record means a file the player put there by hand is
always copied, even when a mod holds the same bytes: their own file must not depend on a mod staying in the Library.
Combined files (ADR-0025), fixes (ADR-0013) and `AddIns.xml` aren't in the Library's mod list, so they are copied too.

### How it comes back
A restore looks for every such file and reads it **before it changes anything**. If one is gone or damaged, the restore
stops with `DAO_MOD_UNDO_NEEDS_REMOVED_MOD`, the folder as it was and the backup kept. It says to remove the mod if it's
still listed and add the download again, since adding a download already in the Library repairs nothing.
Any mod's copy with the right hash will do. Each file then goes through the same temporary file as a saved copy, and its
hash is now checked before that file takes the place of what's there, for saved copies too.

### What doesn't change
The order of plan, backup, journal and apply, and restores that are safe to repeat. Backups made before this keep their
copies and restore as they did.

Manifests are schema 3. A build before this refuses them rather than failing halfway through a restore. It reads every
manifest to list backups, so once one exists that build can't list any of them, as with every schema change before it.

## Consequences
- A round of *Find the problem mod* should write almost nothing to the backups folder, since it only turns mods off.
- *Undo* to mods that have left the Library was already refused (ADR-0039), so that case loses nothing. What is new: a
  library file damaged or deleted by hand now stops an *Undo* or a `recover --rollback` until the download is added again.
  A saved copy did not depend on the Library.
- A restore reads each library file one more time, to find out before changing anything.
- The space existing backups use isn't given back. Nothing removes a backup yet, and what is copied, such as combined files,
  still adds up slowly. That is a separate decision.

## Alternatives considered
- **A button that clears old backups:** simple, but the space still grows until it's pressed, and clearing loses *Undo*.
- **Hard links from the backup to the Library:** no copy and no dependency on the mod list, but they need one drive, and
  the data folder and a moved Library may not share one.
- **Leaving every file with a matching hash to the Library, placed by DAORework or not:** saves a little more, and puts
  a player's own file at the mercy of a mod's removal.
- **Keeping only the last few backups:** loses *Undo* without asking, and backups are the promise of Quality Attribute 1.
