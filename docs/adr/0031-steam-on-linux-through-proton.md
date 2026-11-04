# ADR-0031: Support Steam on Linux, with the game running through Proton

- **Status:** Proposed. Becomes Accepted when the owner schedules Phase 2.7.
- **Date:** 2026-11-04

## Context
The SDD puts Windows first and keeps a Linux port possible, and [ADR-0010](0010-gui-avalonia-in-phase-1.md) chose
Avalonia to keep it so. Dragon Age: Origins has no Linux version: a Linux player runs the Windows game through Proton,
Steam's compatibility layer. DAORework would be a Linux program looking after a game that believes it is on Windows.

A trial on 2026-11-03 published both programs for `linux-x64` from `main`, with no source changes, and ran them in
Ubuntu 24.04 under WSL against a made-up Steam folder of empty files on a case-sensitive disk. It used no real game,
no real Proton and no real Steam.

| Tried | Result |
|---|---|
| Build, and `dao --help` | Works |
| `diagnose`, with the paths named by hand | Works, and the profile passes validation |
| `mods import`, including zips written with `\` in their paths | Works: real folders, as on Windows |
| `mods activate` and `mods rollback`, override mods and a `.dazip` add-in | Work: the add-in lands in `AddIns`, `AddIns.xml` is updated and undone |
| Two files differing only by case | Flagged as a conflict, as on Windows |
| `detect` | Finds no install: the Steam folder comes from the registry or Program Files, and both are absent |
| Documents folder | Not found: the game's Documents folder is inside the Proton prefix |
| A home folder missing `Documents` or `.local/share` | `Environment.GetFolderPath` returns an empty string for a folder that does not exist, so `init` saves the relative path `BioWare/Dragon Age`, and profiles and logs are written into the folder DAORework was started from |
| `launch` on the Steam copy | Recognised as Steam's copy and opens the `steam://` link. WSL has nothing to open it with, so the launch itself was not seen working |
| The window | Crashes at startup without `libICE.so.6`. Avalonia loads it whatever `EnableSessionManagement` or `AVALONIA_X11_USE_SESSION_MANAGEMENT` say. The window has not yet been seen on Linux |

What the community reports, which a real run must confirm: the game's documents folder is
`<library>/steamapps/compatdata/47810/pfx/drive_c/users/steamuser/Documents/BioWare/Dragon Age`; the prefix exists
only once the game has been started; and Steam lives in `~/.local/share/Steam` (also reached through `~/.steam/steam`),
or for Flatpak under `~/.var/app/com.valvesoftware.Steam` (`.local/share/Steam` or `data/Steam`), or for Snap under
`~/snap/steam/common/.local/share/Steam`. A library can be anywhere, such as a microSD card under `/run/media`.

## Decision
### What is supported
- **Steam's copy of the game on 64-bit Linux, run through Proton.** The command line and the window both, from a
  `linux-x64` download.
- **Any other copy is unsupported but not blocked.** A GOG or EA copy under Heroic, Lutris, Bottles or plain Wine can
  be named by hand, and mods and backups work on it. DAORework does not find it or start it.
- **Windows does not change.** Nothing here alters what a Windows player sees.
- **A Steam Deck** counts as Linux on the desktop side. It is called supported only after a real run on one.

### Finding folders
- **An empty known folder is never joined into a path.** `EnvironmentKnownFolders` asks for the profile, Documents and
  local application data folders with `SpecialFolderOption.DoNotVerify`, so a folder that does not exist yet still has
  its name. Program Files and the other Windows-only folders stay empty off Windows, and whatever reads them skips an
  empty one, since `Path.Combine("", "Steam")` would quietly search the current folder.
- **DAORework's own data follows the platform:** `~/.local/share/DAORework`, or under `XDG_DATA_HOME`, which .NET
  already honours. Nothing moves on Windows.
- **Steam's folders are looked for where they really are.** Every Steam root above that exists is read, and each
  root's `libraryfolders.vdf` names the libraries, wherever they are mounted. The player can still name any folder, as
  CLAUDE.md requires. Copies found through two roots are listed as one.
- **The documents folder is found through the prefix.** For each library holding app 47810, DAORework looks in that
  library's `compatdata/47810`, then in the other libraries'. It reports the folder as a new
  `DocumentsFolderSource.ProtonPrefix`, and takes the prefix's user folder from what is on disk rather than assuming
  `steamuser` and `Documents`, which older prefixes name differently.
- **Before the first run there is no prefix.** Diagnostics report a new `DAO_PROTON_PREFIX_MISSING`, and the window
  asks the player to start the game once from Steam. DAORework never creates a prefix or the folders in one.

### Letter case
Linux disks tell `Armor.dds` from `armor.dds`. Wine, under the game, does not. **Names inside the game's folders keep
comparing without regard to case**, as they do today, because that is what the game sees; the conflict warning is then
right on Linux too. DAORework writes the folder names `GamePaths` gives. Whether the real prefix uses those exact
names is checked on the first real run, and a mismatch is a bug to fix there.

### Playing the game
- **A Steam copy starts through Steam**, by the same `steam://` link as
  [ADR-0022](0022-launching-steam-installs-through-steam.md). .NET hands it to `xdg-open`.
- **Any other copy is not started off Windows.** DAORework cannot know which Wine or launcher a player uses, so
  `ProcessGameRuntime` fails with a new `DAO_LAUNCH_NOT_SUPPORTED` and asks the player to start the game from their
  launcher, rather than trying to run a `.exe`.
- **Launch messages stop saying "Windows".** The message names what failed, on either platform.

### Fixes and safety
- **The 4 GB patch** keeps [ADR-0023](0023-steam-protected-executable.md)'s rule. It reads the executable's bytes, so a
  protected copy is refused on Linux as it is on Windows.
- **DXVK is not suggested on Linux.** Its purpose is running Direct3D 9 on Vulkan, and Proton has its own way of doing
  that; replacing it from inside the game folder is more likely to confuse than to help. A DXVK already in the game
  folder is still recognised and can be removed. The real run records what Proton uses for this game.
- **Mods are not scanned for malware on Linux.** AMSI is Windows-only ([ADR-0018](0018-mod-safety.md)), and the import
  already records and says *Not scanned*. The rest of ADR-0018 stands: program files are classified, reported and
  never installed.
- **The prerequisites check** stays Windows-only. DirectX and Visual C++ under Proton are Steam's business.
- **Nothing outside the documents and game folders is touched:** no Proton version, launch option or prefix setting.
  [LEGAL_BOUNDARIES.md](../LEGAL_BOUNDARIES.md) applies unchanged.

### Where the code goes
- `SteamLibraries` (Infrastructure, new) holds finding the Steam roots and reading their libraries.
  `SteamLibraryLocator` uses it, which also keeps that class within the five-dependency limit it has already reached.
- `IProtonPrefixLocator` (Application port) with `SteamProtonPrefixLocator` (Infrastructure) finds the prefix folders
  for `DetectDocumentsFolders`.
- `IHostPlatform` (Application port) says whether DAORework is on Windows, with its adapter in Infrastructure, so rules
  in Application can ask without touching the operating system.
- Core does not change, apart from the new enum value and diagnostic codes.

### Releasing
- **A second download:** `DAORework-<version>-linux-x64.tar.gz`, built by `scripts/publish-release.ps1` on Linux, with
  `DAORework` and `dao` as single self-contained files. A tar file keeps their permission to run; a zip does not.
- **Same trust as Windows, minus signing:** a SHA-256 file and a build attestation. Windows signing does not apply.
- **On GitHub first.** It joins the Nexus page only after a real run, as a separate decision on what the page says.
- **The window names what is missing.** `libice6` and `libsm6` (or the distribution's equivalent) are listed as
  requirements, and `Program.Main` answers a failed library load with one line saying what to install, then exits
  with an error, instead of a stack trace.
- **CI starts the real window.** A job runs the published program under `xvfb` and checks that it stays up, because
  the headless tests never touch X11 and could not have caught the `libICE` crash.

## Consequences
- Linux players get detection, mods, backups, diagnostics and Play, on the store most of them use.
- Every release gains a second build to produce, and every player-facing change a second platform to think about.
- **The owner develops on Windows and has no Linux Steam install.** The exit gate needs a real run, so it needs a
  Linux machine or a tester, as P2.5-08 needs for GOG and EA. `scripts/verify-real-install.ps1` assumes Windows and
  does not cover this.
- Deleting a game's Proton files from Steam removes the prefix, and with it the mods DAORework placed there and any
  saves Steam Cloud has not kept. DAORework's library and backups stay in its own folder, and its records then
  describe files that are gone. Diagnostics and *mods status* must say so plainly.
- Linux players' mods go unscanned, so a player relies on the program-file check, the catalog's download hashes and
  their own care. The window says *Not scanned* each time.
- The fix for empty known folders also helps on Windows if a known folder is ever missing.

## Alternatives considered
- *Run the Windows build under Proton beside the game:* needs no port, but the player must install it into the prefix
  by hand, and Avalonia and .NET under Wine are a second thing to support.
- *Support every Wine launcher from the start:* each keeps its prefixes and its records differently. Steam is one
  layout and most of the players; the others stay possible by naming the folders.
- *Command line only:* the window is the main way to use DAORework, so this would serve the fewest people.
- *Flatpak, AppImage or distribution packages:* better installs, more to maintain and sandboxing to work through.
  A plain archive answers whether anyone wants this first.
- *Bundle `libICE` and `libSM`:* desktop distributions already ship them, and it would add native libraries and their
  licenses to the download for the sake of minimal systems.
- *Scan with ClamAV when it is installed:* a new integration with its own failure cases, for a minority of players.
  Worth an ADR of its own if players ask.
- *Read the prefix's registry for the Documents path:* more exact, but needs a parser for Wine's `.reg` files, for
  what listing one folder already answers.
