# ADR-0012: Mod storage: a library in app-data, copied into DAORework's own subfolders

- **Status:** Accepted
- **Date:** 2026-09-29

## Context
Phase 2 is the first code that writes to the player's folders (ROADMAP P2-01 to P2-08). Everything after this ADR
depends on three answers: where a mod lives once DAORework has it, how it reaches the folders the game reads, and how
DAORework knows afterwards which files are its own. The answers have to make every change previewable, backed up and
reversible (SDD §5, Plan → Apply + Memento), and they must never put at risk a file DAORework did not place.

The game reads two kinds of mod, both under the documents folder (`Documents\BioWare\Dragon Age`):
- **Override mods** are loose files under `packages\core\override`. The game looks files up there by name in any
  subfolder, which is why players put each mod in a folder of its own.
- **Add-ins** come as a `.dazip`: a zip with a `Manifest.xml` and a `Contents` folder. The game's `DAUpdater.exe`
  extracts `Contents` into the documents folder, which puts `Contents/addins/<uid>` under `AddIns\<uid>` and any
  `Contents/packages/...` under `packages\...`. It then adds the manifest's `AddInItem` to `Settings\AddIns.xml`.
  The known-items catalog showed both shapes on real downloads (P1-07).

`DaoModProfile` (Phase 0) has lists of override folders and package paths, but nothing uses it yet.

## Decision

### The library
- Importing a mod (P2-07) extracts it once into `%LOCALAPPDATA%\DAORework\library\<modId>\`, and that copy is never
  changed afterwards. A `mod.json` beside it records: `schemaVersion`, `id`, `name`, `version` (when known), `kind`
  (`Override` or `AddIn`), the source archive's name and SHA-256, the add-in's `AddInItem` for an add-in, and every
  file with its relative path, size and SHA-256.
- `modId` is the lowercased, hyphenated name plus the first 8 hex digits of the archive's SHA-256, so re-importing the
  same archive gives the same id, and two releases of a mod get different ids.

### Deployment: copies, into folders DAORework owns
- Activating a mod **copies** its files. It never uses links, so the documents folder can be on another drive or on
  OneDrive, no elevated rights are needed, and a deployed file changed in the game folder can never damage the library.
  (Chosen by the owner, 2026-09-29.)
- Override files go to **`packages\core\override\DAORework\<modId>\`**, keeping the mod's own relative layout
  underneath. (Chosen by the owner, 2026-09-29.) This covers an add-in's `Contents/packages/core/override/...` as well.
- An add-in's `Contents/addins/<uid>/...` goes to `AddIns\<uid>\...`, where the game expects it. Any other
  `Contents/packages/...` path goes to its exact place. Its `AddInItem` is added to `Settings\AddIns.xml`; deactivating
  removes that entry and nothing else in the file.
- **One file name, one deployed copy.** When two active mods ship an override file with the same name, only the copy
  from the mod later in the profile's order is deployed. Which copy the game would load otherwise is outside
  DAORework's control, so the result is made deterministic instead. The conflict scanner (P2-02) computes this, and
  the plan shows it.

### Ownership: the deployment record
- For each documents folder, `%LOCALAPPDATA%\DAORework\deployments\<documentsKey>.json` records the active mods in
  order, every file DAORework placed (path relative to the documents folder, SHA-256, owning `modId`), and the
  `AddInItem` UIDs it added. `documentsKey` is the first 16 hex digits of the SHA-256 of the documents folder's full,
  normalized path. Profiles that share a documents folder share its record.
- DAORework deletes or overwrites **only files in the record whose current hash still matches it**. A recorded file
  that has since changed stops the plan as a conflict, and the user chooses to keep it or replace it (backed up
  first). A file that is not in the record is never modified, moved or deleted. When such a file has the same name as
  a managed one, it is reported as a conflict the game will resolve on its own.
- The record is written last, atomically, after the applied changes are verified (P2-05). A crash before that point
  is handled by the journal, not the record.

### How every change is made
- Every activation, deactivation or switch between mod profiles builds a `ModActivationPlan` (P2-03). The plan lists
  every copy, delete and `AddIns.xml` edit, and the disk space it needs. It is shown before anything happens and needs
  confirmation. Next, the backup (P2-04) saves every file the plan will overwrite or delete, plus `AddIns.xml`. Then the
  journaled executor (P2-05) applies it.
- **Deactivating everything** removes every recorded file whose hash still matches, removes the owned folders once
  they are empty, and removes the added `AddInItem` entries. That puts the folders back as they were before
  DAORework, apart from anything the user changed in the meantime.
- The install folder and the saves are outside this model. Managed fixes that touch the install (P2-08: the 4 GB flag,
  DXVK) use the same plan, backup and journal path, under that task's own rules.

### Mod profiles
`DaoModProfile` becomes a named, **ordered** list of library `modId`s (schema 2, with a migrator from the unused
schema 1). Activating a mod profile means making the deployment record match it.

## Consequences
- Every deployed mod is stored twice: once in the library, once deployed. Most Dragon Age mods are small, but texture
  packs can be gigabytes, so the plan shows the space needed and checks it is free. Hard links could be added later as
  an option, behind their own ADR.
- Mods placed by hand keep working and are never touched. They do not appear in the deployment record, so DAORework
  can warn about them but cannot manage them until they are imported.
- Deleting DAORework's own subfolder by hand makes the record stale. The next plan finds recorded files missing,
  shows that, and rebuilds the record from what is actually on disk.
- That the game finds override files by name in any subfolder, and so loads `override\DAORework\<modId>\`, is
  community knowledge. The owner confirms it on a real install when P2-06 is verified.
- New persisted documents (`mod.json`, the deployment record, `DaoModProfile` schema 2) follow SDD §8.4:
  `schemaVersion`, atomic writes, migrators.

## Alternatives considered
- *Hard links:* no extra space, but only on the same drive, and an edit through the game folder changes the library copy.
- *Symbolic links:* need Developer Mode or elevated rights on Windows, and some tools handle them badly.
- *A virtual file system, as Mod Organizer 2 does:* it hooks into the game process, which is far beyond this project's scope and legal comfort (LEGAL_BOUNDARIES.md).
- *The mod's own layout directly in `override`:* DAORework's files mix with the user's, and ownership would rest on the record alone.
- *No library, deploying straight from the archive:* a deployed mod could not be restored or redeployed without the original download.
- *One deployment record per runtime profile:* two profiles with the same documents folder would each think they own the same files.
