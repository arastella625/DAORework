# ADR-0015: Adopting hand-installed mods

- **Status:** Accepted
- **Date:** 2026-10-28

## Context
Phase 2.5 (ROADMAP P2.5-01 to P2.5-03) exists so that a player who already mods by hand can switch to DAORework
without reinstalling anything. Today they cannot, because ADR-0012's ownership rule says a file DAORework did not
place is never modified, moved or deleted. That rule is what keeps the user's files safe, and the planner enforces it
in three places:
- an add-in already registered in `Settings\AddIns.xml` blocks the plan (`PlanBlockerKind.AddInAlreadyRegistered`);
- a hand-placed file at a path DAORework would write blocks the plan (`UnmanagedFileInTheWay`), which is what
  happens to an add-in installed by `DAUpdater.exe`, since `AddIns\<uid>\` is also where DAORework deploys it;
- a hand-placed override file with the same name as a managed one is only a warning (`UnmanagedShadow`), so
  importing and activating a mod the user already has by hand leaves **two** copies in `override`, and the game picks
  one.

So the only way in today is to remove every mod by hand and import it again from the original download, which many
players no longer have. Adoption is the missing step: the user hands an existing mod over to DAORework, which from
then on manages it like one it installed.

What a hand-installed mod looks like on disk:
- **An add-in** is an `AddInItem` in `AddIns.xml` and a folder `AddIns\<uid>\`. `DAUpdater.exe` may also have put
  files from the `.dazip`'s `Contents/packages/...` under `packages\`, most often in `override`. Without the original
  `.dazip`, nothing on disk says which of those files belong to which add-in.
- **An override mod** is a set of files somewhere under `packages\core\override`. By convention each mod has its own
  subfolder, but many older mods tell players to drop their files straight into `override`.
- **Official BioWare DLC** is also registered in `AddIns.xml`. It must never be touched: a damaged `AddIns.xml`
  silently disabling DLC is the best-known failure of the existing mod managers (ADR-0009).

## Decision

### The ownership rule gains one exception
ADR-0012's rule stands, with one addition: **a file DAORework did not place may be taken over by an adoption the
user confirmed.** An adoption is a change like any other. It has a plan that names every file it records, moves or
removes, needs confirmation, is backed up, is journaled, and `mods rollback` undoes it. Apart from an adoption,
nothing about the rule changes.

### What can be adopted (P2.5-02, the scan)
The scan reads the documents folder and never writes. It lists **adoption candidates**, each with a stable id the
CLI and the GUI both use:
- `addin:<uid>`: an `AddInItem` registered in `AddIns.xml` whose `AddIns\<uid>\` folder is in the documents folder.
- `override:<folder>`: a top-level subfolder of `override` other than DAORework's own.
- `override:(loose)`: the files directly in `override`, together, as one mod the user names when adopting.
  (Chosen by the owner, 2026-10-28. Many older mods tell players to put their files straight into `override`.)

Every candidate is shown with its file count and size. It also shows its name, the catalog's name and link when a
fingerprint matches (P1-07), and how it was identified (below). The scan also lists what **cannot** be adopted, and
why:
- **Official content is never a candidate.** The known-items catalog gains entries of a new kind, `Official`, with
  the UID of every official DLC add-in. The UIDs are read from the owner's own install, the way P1-07 read mod UIDs.
  An `AddInItem` whose folder is not under the documents folder is never a candidate either. DAORework therefore never
  copies BioWare content into its library, and never edits an official `AddIns.xml` entry.
- An `AddInItem` with no folder, or a folder with no `AddInItem`, is reported as broken, not adopted.
- Files DAORework already records are not candidates. They are already managed.

### How a candidate becomes a library mod
Adoption is used in two ways, tried in order:
1. **Matched to a library mod.** The user has imported the original download, before or during adoption. When every
   file `ModLayout` maps for that library mod is on disk, at its target path or at the path `DAUpdater.exe` would
   have used, with the same SHA-256, the candidate *is* that mod. Nothing new goes into the library. A partial match
   (files missing or changed) is shown as "differs from <mod>: n files". It falls back to the next way and is never
   matched silently. This is the only way an add-in's files outside `AddIns\<uid>\` are adopted with it.
2. **Imported from disk.** Otherwise the candidate's own files are imported into the library, exactly as
   `mods import <folder>` imports an extracted folder (identity: the hash of the sorted file list with each file's
   hash, `FolderModSource`). An add-in's `AddInItem` is copied verbatim from `AddIns.xml` into a `Manifest.xml` in its
   library copy, the shape a `.dazip` has, so re-activating it later registers exactly the entry the user had.
   (Corrected on 2026-10-29 by the owner from "into its `mod.json`": the manifest needs no new schema, and every
   part that reads a `.dazip`'s manifest works unchanged.) `mod.json` gains an `origin` (`Download` or
   `Adopted`, plus the adopted path). That is schema 2, with a migrator that marks every existing entry `Download`
   (SDD §8.4).

Importing writes only to app-data and happens before the plan. A kill after the import and before the plan leaves an
extra library entry, which the next import recognizes, as it does today.

### What adoption does on disk (P2.5-03, the apply)
After an adoption the documents folder is exactly as ADR-0012's layout would have put it, so every later activate,
deactivate and rollback works unchanged. (Chosen by the owner, 2026-10-28.)
- **Files already at their layout target with the same hash** are recorded and not touched. This covers everything
  in `AddIns\<uid>\`. The plan shows them as *Adopt (no change)*.
- **Override files elsewhere** are moved into `override\DAORework\<modId>\`, keeping their layout below the
  candidate's folder. The game looks override files up by name in any subfolder (ADR-0012), so it loads the same
  files as before. Both paths are in the same `override` folder, so a move is a rename. It needs no extra disk space
  and has no partial copy to recover from. This adds a `Move` plan action, with its journal step and its recovery both
  ways.
- **The `AddInItem`** stays as it is in `AddIns.xml`. It is not rewritten. It is added to the record's
  `AddedAddIns`, so deactivating the mod later removes that one entry and nothing else, as for any managed add-in.
- **Two adopted override mods with a file of the same name.** ADR-0012 deploys one copy per name: the one from the
  mod later in the order. Adopted mods are added to the end of the active list in the order the user confirms, and the
  plan names each file that will no longer be in `override` and which mod's copy wins. The user can reorder before
  confirming. Which copy the game loaded before was its own, undefined choice, so the plan says plainly that this can
  change what the game shows.
- The hash of every file is checked again when it is applied. A file changed since the plan was built stops the
  change, as `ChangedSinceDeployed` does today.

### Backup and rollback
- Files that are only recorded need no backup. They are not changed.
- Moved files are not copied into the backup. The backup records each move (from, to, SHA-256), and restoring it
  moves the file back. If the file is no longer where the move put it, the copy comes from the library, found by its
  hash, which ADR-0013 already does for fixes. (Chosen by the owner, 2026-10-28.)
- A removed duplicate (above) is backed up like any deleted file.
- `mods rollback` after an adoption puts every file back where it was, takes the adopted mods out of the record, and
  leaves `AddIns.xml` as it was. The library entries stay, so the user can adopt again without importing again.

### Interface
- CLI: `mods adopt --dry-run` lists candidates and what cannot be adopted. `mods adopt <candidate id>...` shows the
  plan and asks, `--files` lists every file, and `--name` names `override:(loose)`. `--yes` works as for
  `mods activate`.
- GUI: the Mods page shows an *Installed by hand* list with an *Adopt* action (P2.5-02, P2.5-03, P3-04). The first-run
  flow (P2.5-07) offers it when the scan finds candidates.
- New diagnostic codes use the `DAO_MOD_ADOPT_*` area (SDD §8.3). `ModChangeKind` gains `Adopt`, so `mods status` and
  the backup list say which change was an adoption.

## Consequences
- A player with an existing setup can switch without reinstalling, and can undo the switch with one rollback.
- The ownership rule now has an exception. It is narrow, but every future change to the planner has to keep it
  narrow: only an adoption the user confirmed, only the files its plan names, and only while their hashes still
  match.
- `Move` is a new plan action, so it is a new kill point. ADR-0014's fault-injection suite gains adoption scenarios:
  an add-in adopted with no change, an override folder moved, loose files adopted, two folders with a duplicate name,
  and each one rolled back. Each scenario is checked both ways, as the existing ones are.
- The library grows by the size of every adopted mod. This is the same cost ADR-0012 accepted for imports, and the
  plan shows it and checks the space is free.
- An add-in adopted from disk without its `.dazip` brings only `AddIns\<uid>\`. Files its installer put under
  `packages\` stay unmanaged and are listed as "may belong to <add-in>" when a catalog fingerprint says so. Importing
  the original download and adopting again fixes that.
- The `Official` catalog kind is only as good as its UID list. The owner's real-install check (P2.5-08) confirms that
  every DLC on a complete install is excluded before release.
- P2.5 ships as separate PRs: this ADR; the `Official` catalog entries; `mod.json` schema 2; the scan (read-only); the
  `Move` action with its journal and recovery; the adoption plan and apply; the fault-injection scenarios; the GUI.

## Alternatives considered
- *Ask the user to reinstall every mod through DAORework:* the status quo, and the reason players would not switch.
- *Adopt in place, recording files where they are:* no moves, but `ModLayout` would need to remember every adopted
  mod's own paths, and the next activation would move them anyway.
- *Copy into the owned folder and delete the originals:* the same end state as moving, but it needs twice the space
  during the change, and a crash can leave a half-copied texture pack.
- *Report loose `override` files without adopting them:* the user would have to sort them into folders by hand
  first, which is the kind of chore DAORework is meant to remove.
- *Copy every moved file into the backup first:* about three times the space a texture pack needs during the change,
  for a copy the library already holds.
- *Treat every registered add-in as adoptable:* official DLC would be offered, and a mistake there is exactly the
  failure DAORework exists to prevent.
- *Infer an add-in's `packages` files from timestamps or names:* a guess, and a wrong guess would move another mod's
  files.
