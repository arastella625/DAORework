# ADR-0039: Look before adding, and keep one copy

- **Status:** Accepted
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
- Removed copies go to the Recycle Bin, so "nothing is ever lost" holds for a mod removed by mistake, and putting back its
  `mod.json` and folder restores it. Where there is no Recycle Bin, removing is permanent and the preview says so. The
  `mod.json` goes first, so a mod is out of the Library as soon as any of it is gone, and a folder left without one is
  replaced by the next import, as an unfinished import's is today.
- *Undo* refuses a change whose earlier state names a mod no longer in the Library, and says which. It would otherwise
  leave a record that no later plan can use. The preview says so before anything is removed.

**Keep one of each** *(added by P2.6-42)*.
- One action clears every set of copies from a single preview. The rules above apply to each set: nothing here removes
  or swaps in a way *Keep this one* wouldn't.
- **A part counts as a copy.** A mod whose game files, by name and hash, are all inside another counts as part of it, when
  the two have one name once punctuation and case are ignored, or when the bigger one is on, so the smaller adds nothing
  to the game. Moving from Vortex leaves such fragments. The same name with different contents is still not a copy, and
  add-ins stay copies by UID alone, since their files live in their own folder.
- **Nothing is lost by not reading.** A set where one copy holds everything starts on that copy: the one on in the selected
  profile, or else one on in another profile, or else the newest. A whole replaces a part that is on, through the swap. A
  set where no copy holds everything starts on *Keep all of them*, comes first, and goes only if the player picks a copy.
- The player can change any pick, or choose *Keep all of them* to leave a set as it is. The order doesn't change when a
  pick does, so the player keeps their place in the list.
- **The app leads.** When copies can be cleared with nothing lost, that is the next step, after taking over what is in the
  game and before the ordinary path. Its button opens the filled-in preview. Nothing is ever removed without it, since
  removing is permanent and cuts Undo. Sets that really differ are no next step: a player may mean to keep them.
- All swaps go in one plan, applied once, and only then is anything removed.
- A copy can be on and own no file, when another copy placed the same files first. Taking it out moves nothing, so only the
  record changes, with no backup and no entry in History.
- Any swap is refused if what is on has changed since the preview, since the list stays usable under it and the old plan
  would drop that change from the record.
- A set that can't be cleared, because a copy is on in another profile, stays as it is and says why. The others go ahead:
  one stuck set shouldn't hold up fifty. An unfinished change still stops everything.
- Only one of the two previews can be open. Two made before either ran could remove the copy the other keeps.

## Consequences
- The extras can be cleared in one go, and a new duplicate is caught before it costs a copy.
- ADR-0012's rule becomes: a library entry's files never change, but the entry can be removed.
- Removing a copy cuts History off for changes that used it. Copies with the same files lose nothing else.
- A file only in a removed copy can't be restored by an older *Undo* until the copy is back. It fails with the existing
  message and changes nothing. The preview lists those files first.
- The Recycle Bin keeps the space until it is emptied, so the preview says the space comes back then. Windows may delete
  a folder too big for the Recycle Bin outright, and the shell gives no way to tell.
- The swap is an existing kind of change, so ADR-0014 needs no new gate scenario. Removal touches only the Library, and
  its order is covered by tests.
- A copy on in a folder no profile names any more can't be seen, since records can't be listed. Pointing a profile back
  there says the mod isn't in the Library, and adding the same download again settles it.
- A swap copies the kept mod and backs up the removed copy, like any change, though that backup can't be used without
  adding the copy again. Moving the files instead would save both. Until then, keeping the copy that is on costs nothing.
- An entry with nothing for the game, only a readme or a Mac leftover, is part of the mod of its name, so it goes with
  the other copies (P2.6-52). Such entries came from *Take them over*, which no longer makes them.
- The window comes first. `mods keep` for the command line is a later task.
- With 51 sets the sheet scrolls its list, as ADR-0038's does, and takes most of a small window. The total, what differs
  and the buttons stay in view, and the mods list keeps what is left.
- *Keep one of each* removes many mods for good from one press, but the press loses no game file unless the player
  picked a copy themselves. A readme only in a removed copy still goes.
- Whether a smaller mod of another name is a part depends on what is on, so it can stop being one when the bigger mod goes
  off.
- The next step stays until the copies go. A player who keeps identical copies, or has a set stuck on another profile,
  sees it instead of *You're ready to play*, and there is no way to dismiss it yet.

## Alternatives considered
- **Refuse a duplicate outright.** Names can't tell a new version from a copy, so this would block real updates.
- **Import first, then compare hashes.** Certain, but it extracts and scans gigabytes to learn what names can suggest.
- **Stop every add until everything found is managed.** Blocks mods that have nothing to do with what was found.
- **Keep removed copies in a holding folder.** Keeps the 6 GB the owner wants back, and adds a second thing to clear. The
  Recycle Bin is one the player already knows how to empty.
- **Delete for good.** The first version of this ADR did. The owner chose the Recycle Bin, so a mistake can be put right.
- **Refuse to remove a mod any backup names.** Safe, but nearly every copy that was ever on could then never go.
- **Stop the whole clear for one stuck set.** Simple, but the player would have to solve every set before clearing any.
- **A preview per set, one after another.** Keeps each one simple, and is the 51 steps this is meant to remove.
- **Clear copies without a preview.** The fewest clicks, but removing is permanent and cuts Undo.
- **Start sets that differ on the newest.** One click for everything, and a player who doesn't read loses files for good.
- **Treat any mod inside another as a part.** Would catch more, but a small mod with its own name inside a bigger mod that
  is off may be the one the player wants.
