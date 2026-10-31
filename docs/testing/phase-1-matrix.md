# Phase 1 test matrix

The manual record the Phase 1 exit gate asks for ([ROADMAP](../ROADMAP.md#phase-1-companion-v1-read-only-tooling)):
it works on Steam, GOG and EA App installs; no write operation targets any path outside the app-data folder; every
Phase 1 feature is reachable in the GUI; the headless screenshot tests are green.

**Status: Steam verified; GOG and EA App carried forward as an open issue.**
Steam is verified on a real install. GOG and EA App are covered only by automated tests that use a fake registry,
because nobody on the project has a real GOG or EA App copy yet. The owner closed Phase 1 on 2026-09-29 with this item
open, rather than hold Phase 2 for it. The runs go in [How to add a run](#how-to-add-a-run).

## Installs

| Distribution | How DAORework finds it | Real install | Automated tests | Status |
|---|---|---|---|---|
| **Steam** | `libraryfolders.vdf` under the Steam folder, then `appmanifest_47810.acf` in each library (any drive) | Ultimate Edition at `D:\SteamLibrary\steamapps\common\Dragon Age Ultimate Edition`, Windows, owner's machine. All PASS, 2026-09-29 (run below). | `SteamLibraryLocatorTests`, `VdfReaderTests`; the App tests detect a Steam library on a second drive | **Verified** |
| **GOG** | `HKLM\SOFTWARE\WOW6432Node\GOG.com\Games\*` (and the non-WOW64 key), matched by game name, folder checked on disk | Not run | `RegistryLocatorTests.Gog_GameRegisteredUnderGalaxy_IsFound`, `Gog_RegisteredButNotOnDisk_IsNotReported` | **Open** |
| **EA App / Origin / retail** | `HKLM\SOFTWARE\WOW6432Node\BioWare\Dragon Age` `Path` (and the non-WOW64 key), folder checked on disk | Not run | `RegistryLocatorTests.BioWare_KeyWrittenByTheEaApp_IsFoundAsEaApp`, `BioWare_KeyPointingIntoSteam_KeepsTheSteamDistribution` | **Open** |
| Any other folder | `detect --path <folder>`, or Browse on the Profile page | Steam folder given by hand: PASS | `WellKnownPathLocatorTests`, CLI and App tests | Verified |

What the fake-registry tests cannot show: the exact key and value names a real GOG Galaxy or EA App install writes
on a current Windows. The GOG key layout (`GOG.com\Games\<id>` with `gameName` and `path`) and the BioWare key
(`BioWare\Dragon Age` with `Path`) come from the stores' documented and commonly reported registrations, not from an
install we have seen.

## Steam run, feature by feature

`scripts/verify-real-install.ps1 -Branch feat/P1-07-known-items-catalog -SkipLaunch` at `fb56946`, 2026-09-29, with
build and the 404 tests passing first. The game had not yet been launched past its launcher on that machine, so there
are no settings, saves or add-ins yet; the rules that read them report exactly that.

| Feature (task) | Check | Result |
|---|---|---|
| Install detection (P1-01) | `detect` finds the Steam install on `D:` automatically; `detect --path` accepts it | PASS |
| Documents folder (P1-02) | `detect` lists `Documents\BioWare\Dragon Age` (exists) and the OneDrive candidate (not found, marked as synced) | PASS |
| Diagnostic rules (P1-03) | Profile valid, game executable found; 4 GB flag read (`DAO_LAA_DISABLED`); DirectX 9 and VC++ 2005 found; settings, saves and DLC each report their absence; no rule raises an error | PASS |
| Support bundle (P1-04) | Zip written with manifest, profile, diagnostics, inventory and both logs; the Windows user name is absent | PASS |
| Save backups (P1-05) | Real profile answers "no saves"; a throwaway saves folder backed up three times with `--keep 2` leaves two zips with the saves and a manifest; the saves folder is byte-for-byte unchanged; `saves list` shows both | PASS |
| Release build (P1-06) | The `DAORework-win-x64` zip from CI, unzipped: `DAORework.exe` opens, `dao.exe detect` and `dao.exe diagnose` work, no .NET installed separately | PASS (owner, by hand) |
| Known-items catalog (P1-07) | The catalog loads (`DAO_KNOWN_ITEMS_NONE`, DXVK suggested); in a throwaway copy of the install, DXVK 2.3's real x32 DLL is reported as `DXVK v2.3`, its x64 DLL is flagged as unloadable, and removing it brings the suggestion back; the real install is untouched | PASS |
| Profiles and persistence (Phase 0) | `schemaVersion` on the profile, no temp files left, a v0 profile migrates, report names unique | PASS |
| Launch | `launch` of an unknown profile fails without creating a file | PASS |
| Launch of the game and the official launcher | Not run in this pass (`-SkipLaunch`) | To do: one run without `-SkipLaunch` before the gate closes |

## No write outside the app-data folder

Every write in `src/` goes through one of these Infrastructure adapters, and each one's folder comes from
`DaoReworkOptions`, which roots everything at `%LOCALAPPDATA%\DAORework`. File names come from validated
`ProfileName`s (no path separators, no reserved names) or from generated timestamps. Audited 2026-09-29.

| Adapter | Writes to | Name from |
|---|---|---|
| `JsonProfileRepository` (via `AtomicJsonFile`) | `profiles\` | `ProfileName` |
| `JsonDaoModProfileService` (via `AtomicJsonFile`) | `mod-profiles\` | `ProfileName` |
| `JsonDiagnosticReportWriter`, `JsonModInventoryWriter` (via `ReportFileWriter`) | `reports\` | timestamp and random suffix |
| `ZipSupportBundleWriter` | `reports\` | timestamp and random suffix |
| `ZipSaveBackupStore` | `save-backups\{profile}\`; its delete refuses any path outside that root | `ProfileName`, timestamp |
| Serilog file sink (`CliLogging`, `AppLogging`) | `logs\` | fixed prefix and date |

Game, documents and saves folders are only ever read: through `IFileSystem` reads in the rules, matchers and use
cases, and by the save backup, which copies **out**. The P1-05 and P1-07 verify checks confirm it on the real machine
("the saves folder is byte-for-byte unchanged", "the real install was not touched").

## Every Phase 1 feature in the GUI

| Feature | Where in `DAORework.App` | Screenshot (CI) |
|---|---|---|
| Profiles: create, select, edit paths, launch target | Header; **Profile** page with Browse buttons | `00-empty`, `01-profile`, `05-invalid-name` |
| Install detection (P1-01) | **Profile** page, *Detect* and *Use* | `06-detected-install` |
| Documents folder detection (P1-02) | **Profile** page, detected documents folders with *Use* | `07-documents-folder` |
| Diagnostic rules (P1-03) | **Diagnostics** page | `02-diagnostics` |
| Support bundle (P1-04) | **Diagnostics** page, *Export support bundle* | `08-support-bundle` |
| Save backups (P1-05) | **Saves** page | `09-saves` |
| Known-items catalog (P1-07) | **Diagnostics** page | `10-known-items`, `11-dxvk-64-bit` |
| Mod folders and inventory | **Mods** page | `03-mods` |
| Launch | Header, *Launch game* | `04-launched` |

P1-06 is the release packaging itself, not a feature inside the window. The screenshots come from
`DAORework.App.Tests` on Avalonia's headless platform and are uploaded by CI on every pull request; all 23 App tests
are green on `main`.

## How to add a run

On a machine with the GOG or EA App copy installed:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify-real-install.ps1 -InstallDir "<the install folder>"
```

Leave out `-SkipLaunch` so the game and launcher starts are checked too. Add a row with the date, the
distribution and the summary to this file, and open a `docs/` pull request. Once GOG and EA App each have a passing
run, mark the gate met in the roadmap.
