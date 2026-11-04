# ADR-0033: Mods that run inside the game: programs in `bin_ship`

- **Status:** Accepted (owner, 2026-11-04). The owner chose on 2026-11-03 to allow these mods. It amends [ADR-0018](0018-mod-safety.md) §1 and extends
  [ADR-0013](0013-managed-fixes-in-the-install-folder.md).
- **Date:** 2026-11-03

## Context
[ADR-0018](0018-mod-safety.md) §1 says the planner never deploys a program file. Its reason is that the game never
runs a program from `override` or `AddIns`, so a program there is a stray helper or something worse. That reason still
holds for those two folders. It does not hold for `bin_ship`, the folder beside the game's executable, where Windows
loads a DLL named like a system one in place of the system's.

A new kind of mod uses exactly that, and the program is the whole mod. Three on Nexus, as of 2026-11-03:

| Mod | How it is packed | What it installs | Needs |
|---|---|---|---|
| [DAO Lib](https://www.nexusmods.com/dragonage/mods/7052), a plugin loader (1,053 downloads; six mods use it) | A `bin_ship` folder, by its page | `dinput8.dll`, `daolib\addresses\`; plugins in `daolib\plugins\<name>\` | Game 1.05, Visual C++ x86 |
| [Native Controller Framework](https://www.nexusmods.com/dragonage/mods/7051) (2,337 downloads) | A `bin_ship` folder, by its page | `dinput8.dll`, `DAControllerFramework.ini` | An executable with the 4 GB flag: *"MANDATORY"* |
| [Luma](https://www.nexusmods.com/dragonage/mods/7084), HDR and upscaling (73 downloads, first uploaded 31 Oct) | **No `bin_ship` folder:** everything at the archive's root, to extract *next to the game's exe* (0.6.9's zip, read) | `d3d9.dll` (dgVoodoo), `dxgi.dll` (ReShade), a `.addon`, 64-bit DLLs, a `Luma` folder of shaders | Its own `d3d9.dll`, `dxgi.dll` and `dgVoodoo.conf` |

Luma's `.addon` is a DLL under another name, which ReShade looks for by that extension. Its 64-bit DLLs are not
mistakes: the game reaches them over a pipe, through a separate process (`Luma-Upscaler.log`).

**What happens today** (read from the code, not run). `ArchiveClassifier` calls all of these override mods. On *Turn
on*, the DLLs are left out with a warning, and `ModLayout` copies the rest under
`packages/core/override/DAORework/<modId>/`, where nothing reads it. The player gets a mod that shows as on and does
nothing, which is worse than a refusal.

**The pull the other way.** ADR-0018 rejected blocking every mod with a program because *players would install those
by hand instead, which is worse*. That is what happens here: a hand-placed `dinput8.dll` gets no antivirus scan, no
check of its build, no record, and no undo. Mod Organizer 2's
[DAO plugins](https://www.nexusmods.com/dragonage/mods/6725) already deploy `bin_ship` files.

**What makes it dangerous.** A DLL in `bin_ship` runs with the player's full rights the moment the game starts. A
clean antivirus result does not make it safe, and DAORework cannot tell what it does. DXVK is the only program
DAORework places today, and it is recognised by its fingerprint first. This would be the first time DAORework places
code it cannot identify, and the README, the website and the Nexus page all promise that programs inside a mod are
never installed.

**What makes it awkward.** DAO Lib and the controller mod want the same file name, and `bin_ship` has no load order:
Windows loads one `dinput8.dll`. [DAO Lib's docs](https://whocares66-dev.github.io/DAOLib/compatibility.html) give a way
through, one mod at a time: rename the other mod's DLL to `dinput8.chain.dll`. Steam's protected executable cannot take the 4 GB patch
([ADR-0023](0023-steam-protected-executable.md)), so the controller mod's stated requirement cannot be met through
DAORework on Steam, and DAORework neither supplies nor recommends a replacement executable. The authors say to delete
files by hand to uninstall, and the `.ini` is meant to be edited.

## Decision
DAORework installs these mods, behind the rules below (chosen by the owner, 2026-11-03). This reverses ADR-0018 §1 for
`bin_ship` only, and changes a public promise with it. The owner accepts the cost: a bad DLL could one day reach a
player through DAORework. In return, players get the scan, the checks, the record and the undo they do not get by hand.

### 1. A mod for `bin_ship` is recognised, and never half-installed
An archive is a new `ModKind.GameProgram` in either of two shapes, each at its root or under one wrapper folder:
- **It has a `bin_ship` folder.** Its files go to the same place in `bin_ship`. Files outside that folder, such as a
  readme, stay in the library and are not deployed.
- **It has a DLL at that level and no game data anywhere.** Everything goes into `bin_ship` as packed, because no other
  folder could use it. Game data means the file kinds the game reads from `override` (`.erf`, `.dds`, `.gda`, `.ncs`,
  `.tlk` and the like), kept as one list in Core. This is Luma's shape.

Nothing from a `GameProgram` is copied into `override` any more.

A mod with game data as well as programs stays what it is today, with its programs left out, and its preview says the
program part cannot be installed. So a stray DLL in a texture mod is never offered, and ADR-0018 §1 stands for every mod
that is not a `GameProgram`. Installing both parts means two records and two journals, which needs its own decision.

### 2. What may go in
Each of these is checked when the plan is made, and a refusal blocks the whole mod with the file named.
- **Only into `bin_ship`.** Paths keep the mod's layout below it and pass `ModPath.IsSafe`.
- **Of programs, only DLLs.** A file with an `MZ` header must have the DLL flag set in its PE header, whatever its
  extension, so Luma's `.addon` and DAFix's `.asi` pass and a renamed `.exe` does not. A `.exe`, a script or an
  installer is refused. This keeps a replacement `daorigins.exe` out, which
  [LEGAL_BOUNDARIES.md](../LEGAL_BOUNDARIES.md) and ADR-0023 require.
- **At least one 32-bit DLL.** A mod with none cannot load in the game, and `PortableExecutableMachine` already tells
  them apart. A 64-bit DLL beside a 32-bit one is allowed and named under *Details*.
- **Only scanned files.** A program whose scan result is anything but clean is not deployed. ADR-0018 lets an unscanned
  mod through because blocking would make the tool unusable; here it blocks one kind of mod and the risk is far higher.
  This also keeps these mods off Linux, where AMSI does not exist and the player would need a Proton launch option
  that [ADR-0031](0031-steam-on-linux-through-proton.md) says DAORework does not touch.
- **Scanned again when turned on.** The program files are passed to AMSI and checked against their recorded hashes at
  planning time, since the antivirus may know more than it did at import.
- **Never over another file.** A file on disk that is not this mod's, by its hash and the record together, blocks the
  plan: the game's own file, a hand-placed one, another mod's, or a fix's. The record alone is not enough, since a
  real install was found with a record naming DXVK and another `d3d9.dll` on disk. There is no *Arrange* for `bin_ship`,
  and the message names both sides. A `d3d9.dll` the catalog knows as DXVK points to *Fixes*, as today.
- **A new version is another library mod.** The old one is turned off first, and the block says so. Luma went from
  0.6.9 to 0.7.1 in about a day, so this message will be read often.
- **No renaming, in this first version.** The catalog's conflict entry for two such mods carries the reason and a link
  to the author's way through, such as DAO Lib's `dinput8.chain.dll`. Doing the rename for the player comes later.

### 3. The player says yes to the program, separately
ADR-0018 rejected asking each time because *players who do not know what a `.dll` is click yes*. That was about a
stray file nobody asked for. Here the player went looking for this mod, and the question is whether they trust its
author, which they can answer. The prompt is also not the only protection: section 2 still applies, and it is off
until they turn it on.

> **Turn on Native Controller Framework**
> Copies 2 files into your game folder.
> ⚠ This mod runs its own program inside the game, with the same access to your computer as you have. Your antivirus
> found nothing, but DAORework can't tell what it does.
> ☐ I trust this mod and where I got it
> [Apply] [Cancel] Details

- *Apply* stays off until the box is ticked. It is never ticked for the player, and it is asked every time the mod is
  turned on, so nothing needs saving.
- *Turn on selected* leaves these mods out and names them.
- On the command line, `--yes` does not cover them. `mods activate` needs `--allow-programs <modId>`, naming each one,
  so a script cannot do this by accident.
- *Undo* can put such a mod back without the tick: it restores the same files, by hash, that the player already
  accepted, and *Undo* is confirmed first.
- A catalog match is shown as it is today. A download that differs stays a warning, because these mods update often.

### 4. Same path as a fix
These files live in the install folder, so everything ADR-0013 settled applies: one record per install folder, a
backup and journal beside it, `DAO_FIX_INSTALL_NOT_WRITABLE`, and `DAO_FIX_FILE_IN_USE` when the game is running.
- The library copy stays in the mod library, so the import, scan and known-download code is reused unchanged.
- Turning off deletes each recorded file whose hash still matches. An edited `.ini` is left in place and named.
  Files the mod wrote while running are not in the record: they stay, and folders are removed only when empty. This
  is common: Luma compiles shaders into its own folder, 137 files on a real install from fewer than 80 in its zip.
- The mod counts as on while its **program files** match the record, so editing an `.ini` does not read as *undone*.
- *History* lists these changes with the rest. The mod sits on the *Mods* tab, tagged *Runs in the game*. There is no
  new tab ([ADR-0026](0026-workflow-first-layout.md)).

### 5. Saying what a mod needs
The catalog gains an optional `needs` list of item ids, with a source link as conflicts have. The preview warns when a
needed item is missing. On Steam's protected executable the warning says the 4 GB patch cannot be applied there and
that the author says the mod may crash, and stops: it points to no replacement executable (ADR-0023). It stays a
warning, so a player who has sorted out their own executable is not stopped.

### 6. Seeing what is already there
A new read-only diagnostic, `DAO_INSTALL_PROGRAM_FOUND` (info), reports a DLL in `bin_ship` that is neither the game's
nor in the record, by name and with the catalog's name for it when it has one. Taking such a file over (*Manage*) stays
out, as ADR-0018 §1 says for adoption. A player with a hand-placed `dinput8.dll` is told why a mod that wants the same
file is blocked.

The game's own DLLs, from a real Steam install: `binkw32`, `DAOriginsGDF`, `DAUpdater.Engine`, `fmod_event`, `fmodex`,
`icsharpcode.sharpziplib`, `msvcp71`, `msvcr71`, `nxcharacter`, `nxcooking`, `speedtreert`, `umbra` and `unrar`. None
is a name these mods use. GOG and EA copies are compared with this list before it ships.

### Where the code goes
- **Core:** `ModKind.GameProgram` and `ArchiveShape.GameProgram`; `GameProgramLayout`, pure, for section 2's path and
  type rules. `ProgramFiles` does not change.
- **Application:** new use cases `PlanGameProgramMod` and `ApplyGameProgramMod`, sharing the fix path's record, journal
  and backups. `PlanFix` is already at the five-dependency limit and `IManagedFix` is a fixed list of ids, so this is
  not another fix. A refactor PR comes first if a shared piece is fix-only. Fix commands skip record entries that
  are not fixes.
- **New codes:** `DAO_MOD_SAFETY_PROGRAM_REFUSED`, `DAO_MOD_SAFETY_PROGRAM_NOT_SCANNED`,
  `DAO_MOD_PROGRAM_PATH_TAKEN`, `DAO_MOD_PROGRAM_NOT_ACCEPTED`, `DAO_INSTALL_PROGRAM_FOUND`.
- **Persisted:** `kind` gains a value, so `mod.json` needs no new schema. A mod already imported stays an override mod
  until the player adds it again.

## Checked on 2026-11-03
Against a real Steam install, with Luma and its stack placed by hand, and the authors' own docs. This check changed
sections 1, 2 and 6: the first draft expected every such mod to have a `bin_ship` folder, refused 64-bit DLLs and
`.addon` files, and trusted the record alone, so it would have refused or half-installed Luma.
- **What the archives hold:** Luma's zip was read. DAO Lib's and the controller mod's are taken from their pages and
  DAO Lib's docs, and the first import of each confirms it.
- **The game's own DLLs:** listed in section 6.
- **DAO Lib's plugins:** `bin_ship\daolib\plugins\<name>\<name>.dll`, which fits. It writes `loader.log` to the
  documents folder, not to `bin_ship`.
- **Proxy DLLs load from `bin_ship`:** `ReShade.log` shows ReShade loading from there and its add-on loading after it.

## Not yet seen
Nobody on the project has run these on a Steam copy whose executable still has Steam's protection (a `.bind` section).
They need a tester with such a copy before Phase 2.8 is released, and nothing here is claimed for that copy until then:
1. Does the game reach the main menu when started through Steam, with a proxy DLL loading? ADR-0023's load error came
   from changing the executable. A DLL beside it should be different, but nobody has seen that here.
2. Does Steam's *Verify integrity of game files* leave the added files alone?

## Consequences
- Native mods can be scanned, previewed, recorded and undone, which they cannot be today.
- **The promise changes.** On release, the words *programs are never installed* change in the README (three places),
  `site/public/index.html`, `docs/release/nexus/description.bbcode` and `ModActivationPlanWarnings`, and the last line of
  ADR-0018's first consequence is amended. `docs/VOICE.md` gains *Runs in the game*.
- ADR-0009's tier 2 speaks of permissively licensed fixes. These are files the player downloaded, as any mod is, and
  nothing is redistributed, so [LEGAL_BOUNDARIES.md](../LEGAL_BOUNDARIES.md) is met without a change.
- [ADR-0014](0014-fault-injection-exit-gate.md): turning a `GameProgram` on and off are new kinds of change, so each
  adds a fault-injection scenario.
- Steam players can install the controller mod but not meet its stated requirement through DAORework. The preview says
  so; it does not solve it.
- DAO Lib and the controller mod cannot both be on until DAORework can do the rename DAO Lib's docs describe.
- A player who installed Luma by hand gets no help yet: its files block the managed copy, and DAORework cannot take them
  over. Since switching without starting again is the reason to use DAORework, adopting hand-placed programs is the
  first thing to decide after this.
- Mixed mods, renaming, adopting a hand-placed DLL and Linux stay out. Each can come later with its own decision.

## Alternatives considered
- *Keep "never", and have the preview say the mod must be installed by hand:* no new risk, but no scan or undo for these
  mods, and new mods built on DAO Lib stay outside DAORework.
- *Allow only downloads the catalog knows by hash:* the safest way to say yes, but the catalog ships inside DAORework,
  which never goes online, and these mods update faster than it does: the controller mod reached 2.2.0 within a month.
  Players would be held to old versions, or go back to installing by hand.
- *Deploy at launch and restore when the game closes, as the MO2 plugins do:* DAORework would have to stay running and
  see the game exit, but a Steam copy is started through Steam
  ([ADR-0022](0022-launching-steam-installs-through-steam.md)), and a crash could leave the files behind unrecorded.
- *Make each mod a fix:* `IManagedFix` is a short list of fixes known at build time, and the players' mods are not.
- *Show the Authenticode signature in the preview:* cheap to read, but if most of these DLLs are unsigned, the line
  teaches players to ignore it.
- *Run the DLL in a sandbox, or inspect its imports:* DAORework cannot judge what native code does, and a check that
  looks like it can is worse than saying so.
- *Rename a DLL automatically when two mods collide:* only one author has said how, for one name, and only for one such
  mod at a time. It belongs in the catalog per pair of mods, not in a general rule.
- *Only accept a mod with a `bin_ship` folder:* the first draft's rule. Luma, the newest of the three, has none, and
  would be half-installed or refused.
