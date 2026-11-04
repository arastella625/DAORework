# ADR-0032: Find the documents folder from the profiles, then by searching the drives

- **Status:** Accepted
- **Date:** 2026-11-03

## Context
`DetectDocumentsFolders` tries three places: the Documents folder Windows reports, `%UserProfile%\Documents` and
`%OneDrive%\Documents`. The owner's folder is `D:\BioWare\Dragon Age`, so `detect` reports nothing, a new profile gets
a folder that does not exist, and every check that reads the folder has nothing to read
([#74](https://github.com/arastella625/DAORework/issues/74)). `CLAUDE.md` says to find a folder where it really is.
`scripts/check-vortex.ps1` already does, by reading the profiles and searching the drives.

Detection runs often: `detect`, a new profile, the window's *Detect* button, and the documents check in every run of
`diagnose`. Walking a player's disks is slow on a hard drive, and it reads the names of their folders.

## Decision
- **Two more sources, cheapest first.** After the three usual places come the folders the saved profiles name
  (`DocumentsFolderSource.Profile`), then a search of the drives (`DocumentsFolderSource.DriveSearch`).
- **The search is a last resort.** It runs only when no earlier source found an existing folder. A folder it finds goes
  into the new profile, so from then on the profile names it and the search does not run again.
- **The order does not change.** Existing folders come first, then by source, so a folder in the place Windows reports
  stays the recommendation. A folder a profile names never displaces it for a new profile.
- **The search has limits.** It looks only on fixed drives that are ready, for `BioWare\Dragon Age` in the drive's root
  and in folders up to three deep, which covers `D:\BioWare\Dragon Age` and `D:\Users\Me\Documents\BioWare\Dragon Age`.
  It skips `Windows`, `Program Files`, `Program Files (x86)`, `ProgramData`, `$Recycle.Bin` and
  `System Volume Information` at the drive's root, and does not follow links, so it cannot loop or walk a drive twice.
  It reads folder names only, never files, and nothing leaves the machine.
- **It sits behind a port.** `IDocumentsFolderSearch` is in Application and `DriveDocumentsFolderSearch` in
  Infrastructure, as with `IInstallLocator`. It runs off the calling thread, so the window stays responsive.
- **A profile that cannot be read names no folder.** Detection carries on without it. The commands that use that
  profile report the problem.
- **The user can still name it**, with `profile set-path <name> documents` or *Game folders* in Settings.

## Consequences
- A player who moved the folder gets it found with no setup, and a new profile points at it.
- On a PC where the game has never run, nothing exists to find, so each detection walks the drives. On the owner's PC,
  with four fixed drives, that took about five seconds the first time and well under a second after. Results are not
  cached, because a cache would need rules for when the folder appears.
- A copy kept as a backup, such as `E:\Backup\BioWare\Dragon Age`, is listed like any other, and is the recommendation
  if nothing else exists. Every candidate is shown with its path, so the player can choose another.
- The test hosts that run on the real file system turn the search off, so a test never reads the machine's drives.

## Alternatives considered
- **Always search.** Simpler, but it would walk the disks on every `diagnose` for every player, to find nothing new.
- **Search only when asked (`detect --search`).** Keeps detection fast, but the player who needs it is the one who does
  not know the folder is missing.
- **Read the folder from the game's own records.** Its settings live inside the folder being looked for, and the
  registry keys DAORework reads hold only the install folder.
