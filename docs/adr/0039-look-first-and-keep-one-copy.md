# ADR-0039: Look before adding, and keep one copy

- **Status:** Proposed
- **Date:** 2026-11-06

## Context
The owner's library holds 139 entries, 51 sets of copies and 6.04 GB of extras. ADR-0037 stopped a mod being copied into
the game twice, but two gaps remain:

- *Add mods* and *New downloads* add a mod that is already in the game or the Library. Nothing offers *Manage* first.
- A copy can't be removed. Nothing deletes from the Library at all, so P2.6-18 could only say a copy exists.

Removing is new. ADR-0012 says a library entry's files never change, and Undo depends on the Library in two ways: a
record that names a mod needs that mod to plan anything, and a moved file can come back from the Library by its hash.

## Decision
**Look before adding.**
- Every way of adding a mod reads the download's file names first. As in ADR-0034 §2, nothing is extracted, hashed or
  scanned. A folder's names are read the same way.
- A download looks already held when every game file it holds has the name of a file held elsewhere, or it is an add-in
  whose UID is held. `chargenmorphcfg.xml` doesn't count, since hair and head mods all ship it (ADR-0025).
- What is in the game and not managed comes first: the player is offered *Manage it*, *Add anyway* or *Cancel*. What is
  already in the Library offers *Add anyway* or *Cancel*. Other downloads in the same batch are added straight away.
- Loose files are managed all together, so *Manage it* is offered for them only when they are this download's files and
  nothing else.
- Names can't prove two mods are the same, so the window says *looks like* and never refuses. An archive that can't be
  listed, or a `.dazip` wrapped in a zip, is added as before, and the row says it is a copy once it is in.

**Keep one copy.**
- On a mod with copies, *Keep this one* removes the other copies from the Library after a preview. It says what is
  removed, how much space comes back, and which files exist only in a copy being removed.
- A copy on in the selected profile is swapped for the kept one, in the same place in the order, through the ordinary
  plan, backup and journal. It is removed only once that change has been applied.
- A copy on in another profile's documents folder stops the removal, and the preview names the folder. So does an
  unfinished change. This is checked again at the moment of removal.
- Removing is permanent. The `mod.json` goes first, so a mod is out of the Library as soon as any of it is gone, and a
  folder left without one is replaced by the next import, as an unfinished import's is today.
- *Undo* refuses a change whose earlier state names a mod no longer in the Library, and says which. It would otherwise
  leave a record that no later plan can use. The preview says so before anything is removed.

## Consequences
- The extras can be cleared one set at a time, and a new duplicate is caught before it costs a copy.
- ADR-0012's rule becomes: a library entry's files never change, but the entry can be removed.
- Removing a copy cuts History off for changes that used it. Copies with the same files lose nothing else.
- A file only in a removed copy can't be restored by an older *Undo*. It fails with the existing message and changes
  nothing. The preview lists those files first.
- The swap is an existing kind of change, so ADR-0014 needs no new gate scenario. Removal touches only the Library, and
  its order is covered by tests.
- A copy on in a folder no profile names any more can't be seen, since records can't be listed. Pointing a profile back
  there says the mod isn't in the Library, and adding the same download again settles it.
- A swap copies the kept mod and backs up the removed copy, like any change, though that backup can't be used without
  adding the copy again. Moving the files instead would save both. Until then, keeping the copy that is on costs nothing.
- The window comes first. `mods keep` for the command line is a later task.

## Alternatives considered
- **Refuse a duplicate outright.** Names can't tell a new version from a copy, so this would block real updates.
- **Import first, then compare hashes.** Certain, but it extracts and scans gigabytes to learn what names can suggest.
- **Stop every add until everything found is managed.** Blocks mods that have nothing to do with what was found.
- **Keep removed copies in a holding folder.** Keeps the 6 GB the owner wants back, and adds a second thing to clear.
- **Refuse to remove a mod any backup names.** Safe, but nearly every copy that was ever on could then never go.
