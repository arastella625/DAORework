# Phase 2.5 test matrix

The manual record the Phase 2.5 exit gate asks for ([ROADMAP](../ROADMAP.md#phase-25-launch-ready-mod-manager)):
a hand-built mod setup adopted on a real install and rolled back, the antivirus scan seen refusing a test file, and
the window used without the command line. Fill in each **Result** after the run, then the gate items can be ticked.

**Status: Steam run done by the owner on 2026-10-31, on the 0.3.0 release candidate; she reports every check below
passed.** The results are her summary of the whole run, not step-by-step notes. The run found one real problem, fixed
before release: Steam refused to start the game after the 4 GB patch, because Steam's `daorigins.exe` is protected
([ADR-0023](../adr/0023-steam-protected-executable.md)); she confirmed the fix on the same machine (section 4, step 1).
The automated suites (unit, App screenshot and fault-injection tests) are green on `main`.

## Installs

| Distribution | Real install | Status |
|---|---|---|
| **Steam** | Ultimate Edition at `D:\SteamLibrary\steamapps\common\Dragon Age Ultimate Edition`, owner's machine | **Run 2026-10-31: passed** (owner's report) |
| **GOG** | Nobody on the project has a GOG copy | Open, as in Phase 1 ([#5](https://github.com/arastella625/DAORework/issues/5)) |
| **EA App** | Nobody on the project has an EA App copy | Open, as in Phase 1 ([#5](https://github.com/arastella625/DAORework/issues/5)) |

The exit gate says no platform is carried forward again. If 0.3.0 ships with GOG and EA App still open, the
release notes and the Nexus page say plainly that only Steam has been run on a real install.

## 1. The scripted checks (about 10 minutes)

On the owner's machine, from a checkout of `main` (or the release tag), with the game closed:

```powershell
.\scripts\verify-real-install.ps1 -InstallDir "D:\SteamLibrary\steamapps\common\Dragon Age Ultimate Edition"
```

It activates two small test mods on the real documents folder and rolls them back, applies the 4 GB patch and DXVK
and removes them, and checks every file is byte-for-byte as it was. Paste its summary here.

| Check | Result |
|---|---|
| `verify-real-install.ps1` summary | Passed (owner's report; the summary was not pasted) |

## 2. Adopting the real setup, in the window only (about 20 minutes)

Before starting, back up the documents folder (`Documents\BioWare\Dragon Age`) by hand, outside DAORework, so
nothing depends on DAORework's own backups. Then use only `DAORework.exe` from the release zip:

| # | Step | Expected | Result |
|---|---|---|---|
| 1 | Start with no profiles (or a new Windows user) and create one | It finds the install and documents folder, runs diagnostics, and Next steps appear on the Profile tab | PASS (owner's report, 2026-10-31) |
| 2 | Next steps: *See them* (adopt) | The Mods tab lists what is installed by hand: Qwinn's Fixpack as an add-in, another add-in, and the multi-folder override mod; official DLC is never offered | PASS (owner's report, 2026-10-31) |
| 3 | Adopt each one (the loose override files need a name) | Each shows its plan first; after Apply it is in the Library as active, and Changes lists a backup | PASS (owner's report, 2026-10-31) |
| 4 | Launch the game from the header and load a save | The mods behave exactly as before adopting | PASS (owner's report, 2026-10-31) |
| 5 | Load order: move one mod that shares a file with another, then Back to the saved order | The winner changes in the preview only; nothing changes on disk | PASS (owner's report, 2026-10-31) |
| 6 | Deactivate one adopted mod, then roll that change back in Changes | Its files leave and come back; the game behaves as before | PASS (owner's report, 2026-10-31) |
| 7 | Roll back every adoption, oldest backup last | The documents folder matches the hand-made backup from before step 1 (compare with a folder diff tool) | PASS (owner's report, 2026-10-31) |

## 3. Mod safety on Windows (about 5 minutes)

| # | Step | Expected | Result |
|---|---|---|---|
| 1 | Make a zip holding the standard EICAR test file, made on the spot from its published 68-character string (never committed, delete it afterwards), and import it in the Library | Refused with `DAO_MOD_SAFETY_THREAT_FOUND`; nothing of it is in the library | PASS (owner's report, 2026-10-31) |
| 2 | Import a zip with a small `.exe` inside (any harmless program) and activate it | Imported with the program named; activating never puts the `.exe` in the documents folder | PASS (owner's report, 2026-10-31) |
| 3 | Import Extra Dog Slot or Qwinn's Fixpack straight from its Nexus download | "Matches the known download of …" | PASS (owner's report, 2026-10-31) |
| 4 | Import a `.7z` and a `.rar` mod | Both import like a `.zip` | PASS (owner's report, 2026-10-31) |

## 4. Fixes and saves in the window (about 5 minutes)

| # | Step | Expected | Result |
|---|---|---|---|
| 1 | Steam install, Fixes tab: apply the 4 GB patch | Refused as Steam's protected copy (`DAO_FIX_STEAM_PROTECTED`, ADR-0023); Diagnostics shows `DAO_STEAM_EXE_PROTECTED`, and Next steps offers no 4 GB patch. If the patch was applied earlier, Diagnostics warns `DAO_STEAM_EXE_PATCHED`, and removing it lets Steam start the game again | PASS: Diagnostics warned about the patched copy, removing it let Steam start the game, and applying it again was refused (owner, 2026-10-31) |
| 1b | DXVK installed, then Steam → Verify integrity of game files, then back in DAORework | DXVK shows as no longer in effect, naming Verify integrity; installing it again restores it | PASS (owner's report, 2026-10-31) |
| 2 | Fixes tab: install DXVK from a downloaded release, then roll it back from its backup | The game starts with DXVK, then without it | PASS (owner's report, 2026-10-31) |
| 3 | Saves tab: back up the saves | A backup appears; the saves folder is unchanged | PASS (owner's report, 2026-10-31) |

## How to add a run

Copy a section's table, fill in **Result** with PASS or FAIL and a short note, and add the date, the commit or tag,
and the machine (Windows version, GPU) above it. A FAIL becomes an issue before the release goes out.
