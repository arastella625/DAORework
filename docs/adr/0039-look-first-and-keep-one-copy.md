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

**Keep one of each** *(added by P2.6-42)*.
- One action clears every set of copies from a single preview. The rules above apply to each set: nothing here removes
  or swaps in a way *Keep this one* wouldn't.
- Each set starts on the copy that is on in the selected profile, or else one on in another profile, or else the newest.
  Starting on what is on means a default clear rarely changes the game, and another profile rarely stops it.
- The player can change a set's pick, or choose *Keep all of them* to leave that set as it is.
- Sets whose copies aren't identical come first, however they are picked, and say what would go. The order doesn't change
  when a pick does, so the player keeps their place in the list.
- All swaps go in one plan, applied once, and only then is anything removed.
- A set that can't be cleared, because a copy is on in another profile, stays as it is and says why. The others go ahead:
  one stuck set shouldn't hold up fifty. An unfinished change still stops everything.
- Only one of the two previews can be open. Two made before either ran could remove the copy the other keeps.

## Consequences
- The extras can be cleared in one go, and a new duplicate is caught before it costs a copy.
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
- With 51 sets the sheet scrolls its list, as ADR-0038's does, and takes most of a small window. The total, what differs
  and the buttons stay in view, and the mods list keeps what is left.
- *Keep one of each* removes many mods for good from one press. The preview leads with what would be lost, but a set whose
  copies differ still starts on a pick, so a player who doesn't read it can lose those files.

## Alternatives considered
- **Refuse a duplicate outright.** Names can't tell a new version from a copy, so this would block real updates.
- **Import first, then compare hashes.** Certain, but it extracts and scans gigabytes to learn what names can suggest.
- **Stop every add until everything found is managed.** Blocks mods that have nothing to do with what was found.
- **Keep removed copies in a holding folder.** Keeps the 6 GB the owner wants back, and adds a second thing to clear.
- **Refuse to remove a mod any backup names.** Safe, but nearly every copy that was ever on could then never go.
- **Stop the whole clear for one stuck set.** Simple, but the player would have to solve every set before clearing any.
- **A preview per set, one after another.** Keeps each one simple, and is the 51 steps this is meant to remove.
