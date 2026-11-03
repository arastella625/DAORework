# ADR-0028: Taking over from Vortex

- **Status:** Proposed (waiting on the real check in §6)
- **Date:** 2026-11-03

## Context
Many players who would switch to DAORework installed their mods with Vortex. Today DAORework notices that Vortex has
deployed into `override` or `AddIns` (it finds a `vortex.deployment*.json` there, P2.5-02). It then tells the player to
purge the mod in Vortex before managing it here. That advice defeats itself: purging deletes Vortex's files from the
game, so nothing is left to manage, and the player has to find every download again.

What Vortex does, from its public source and documentation (GPL-3: behaviour only, no code, per LEGAL_BOUNDARIES):
- **Staging folder:** Vortex keeps each mod in its own folder under a staging folder (by default
  `%APPDATA%\Vortex\dragonage\mods`). It deploys each file into the game as a link to that copy (a hard link by
  default).
- **Where it deploys:** its Dragon Age extension, archived on 2026-05-20, deploys into
  `Documents\BioWare\Dragon Age\packages\core\override`.
- **AddIns.xml:** it merges the `AddInItem`s from each `.dazip`'s `Manifest.xml` into `Settings\AddIns.xml`. Forum
  reports say it can replace the file, so official DLC disappears from the game.
- **Its record:** Vortex lists what it placed, and from which mod, in a record file (`vortex.deployment.json`, or
  `vortex.deployment.<type>.json`) in the folder it deploys to. It also keeps a backup of that record.
- **It puts things back:** before every deploy, Vortex compares the game with its record. If a placed file was deleted
  or moved, its default answer is to put the link back. So moving Vortex's files away, as adopting does, lasts only
  until Vortex next deploys.

The owner chose a guided handover (2026-11-03) over DAORework editing Vortex's files itself.

## Decision

### 1. Vortex's record is read, never written
- **The reader:** a new reader (Infrastructure) parses every `vortex.deployment*.json` that the check in §6 finds in
  the documents folder. For each placed file it reads `relPath` and `source` (the Vortex mod it came from). From the
  record it reads `stagingPath` and `deploymentMethod`. Other fields are ignored.
- **A record that can't be read:** it gets a new diagnostic code, and its folder is treated as it is today.
- **Hands off Vortex:** DAORework never writes, renames or deletes Vortex's records, its staging folder or its
  settings. It never starts Vortex.

### 2. *Found in your game* names Vortex's mods
- **By name:** mods Vortex placed are listed under their Vortex names, tagged *Placed by Vortex*, with how many files
  each placed. They replace today's one warning per folder.
- **Not with *Manage*:** these mods do not offer *Manage*, because Vortex would undo the move. They offer the handover
  below instead.

### 3. The handover, in three steps
A card on the Mods page, *Move from Vortex*, shows the three steps and where the player is. Each step can be left
and picked up later, and the card works out the current step from the game folder each time it opens.

1. **Copy (DAORework).** Each Vortex mod is added to the library as a real copy. That breaks the link, and nothing in
   the game changes.
   - **Where the copy comes from:** the mod's staging folder when it can be read, since that holds the whole mod,
     including files that lost a conflict. Otherwise the files Vortex placed.
   - **The order:** for a file several mods have, the record's `source` is the copy the game uses. The copies are
     arranged so the same copies win.
   - **Add-ins:** an add-in takes its `AddInItem` from the current `AddIns.xml`, as adopting does (ADR-0015).
   - **Kept aside:** `AddIns.xml` as it is now is kept beside the copies, for step 3.
   - **Checks:** copying runs the same checks as *Add mods*: the scan, programs never installed, and the known
     downloads.
2. **Purge in Vortex (the player).** The card gives the steps: open Vortex, choose Dragon Age: Origins, *Purge* on
   the Mods page, then stop managing the game. *Check again* reads the game folder. The step is done when no record
   lists a file and none of the files Vortex placed are left.
3. **Turn them on (DAORework).** One preview turns the copied mods on in the recorded order. It is applied with one
   backup, and *Undo* undoes it, like any change.
   - **AddIns.xml after the purge:** if the purge left `AddIns.xml` missing official DLC that the kept-aside file
     had, the preview says so and puts those entries back.
   - **Undo:** undoing the change never deletes the copies.

Stopping halfway is safe:
- **After step 1:** the game is unchanged, and the copies can be deleted from the library.
- **After step 2:** the game has no Vortex mods until step 3.

### 4. If Vortex deploys again
If a record lists files again after the handover, because Vortex was left managing the game, Home shows a suggestion
(*Vortex is placing mods again*). It tells the player to stop managing the game in Vortex, and links to the card.

### 5. Words and the command line
- **Words:** *Move from Vortex*, *Placed by Vortex*, *Copy*, *Purge in Vortex*, *Turn them on* (VOICE.md gains the
  first two).
- **Command line:**
  - `mods vortex` lists the record's mods and the current step.
  - `mods vortex copy` does step 1.
  - Step 3 is `mods activate` with the copied mods, in the order `mods vortex` prints.

### 6. A real check comes first
`scripts/check-vortex.ps1` is run on the owner's PC, where Vortex has deployed Dragon Age mods. It only reads. It
reports:
- where the record files are, their fields, and the mods they name;
- whether the placed files are still links;
- the staging folder's mod folders;
- what Vortex set aside (`*.vortex_backup`);
- how many add-ins `AddIns.xml` lists.

This ADR is amended with what the check finds, and then accepted:
- whether there is a record for add-ins or for `Settings\AddIns.xml`, and where;
- how a `.dazip` sits in the staging folder;
- whether the merged `AddIns.xml` is a placed file.

Only then are the reader and the handover built.

## Consequences
- A Vortex player switches without finding their downloads again, and Vortex is never left half-managing the game.
- The handover takes two windows and one step done by hand. The card makes that step plain, and checks it rather than
  trusting it.
- Copying needs room for a second copy of each mod. The copy step says how much before it starts.
- The record is undocumented and the Dragon Age extension is archived, so a future Vortex could change it. A record
  that can't be read falls back to today's behaviour, and the real check is repeated before each release that
  changes this code.
- The window gains a card and a tag, not a tab (ADR-0026 §2).

## Alternatives considered
- **DAORework removes Vortex's links and edits its record:** one click, but it writes into another program's files,
  and Vortex keeps a backup of its record and puts links back by default.
- **Adopt (move) the placed files as today:** Vortex puts them back on its next deploy.
- **Read Vortex's own database for mod names and order:** it is an undocumented LevelDB that Vortex locks while it
  runs. The record and the staging folder hold what is needed.
- **Tell the player to purge, then re-add every mod by hand:** the problem this ADR exists to solve.
- **Mod Organizer 2:** it keeps mods outside the game folder (a virtual folder), so nothing it manages sits in
  `override`. Taking over from it would be a separate ADR.
