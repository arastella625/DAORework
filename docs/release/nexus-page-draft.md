# Nexus Mods page: draft for the owner

ROADMAP P2.5-09 asks for a Nexus page **written by the owner**. This is a starting point with the facts in one place,
to rewrite in your own voice before upload. Nexus descriptions use BBCode; the headings below map to `[size=5]` or
`[b]` lines, and the lists to `[list][*]`. Check every claim against the release you upload.

---

## Name

DAORework: mod manager, launcher and diagnostics for Dragon Age: Origins

## Short summary (Nexus allows about 350 characters)

A mod manager and launcher for Dragon Age: Origins. It takes over the mods you already installed by hand without
reinstalling them, installs new ones with a plan you see first and a backup you can roll back to, checks every mod
with your antivirus, and applies the 4 GB patch and DXVK safely.

## Description

**What it does**
- **Takes over your existing setup.** It finds the mods you installed by hand in `override` and `AddIns` and adopts
  them without reinstalling anything. Official DLC is never touched.
- **Installs mods safely.** Import a `.dazip`, `.zip`, `.7z` or `.rar` (or drop it on the window). Activating shows
  exactly what will be copied, replaced or removed before anything changes, and takes a backup first. Any change can
  be rolled back.
- **Shows conflicts.** For every file two mods share, it shows whose copy the game uses, and lets you reorder them.
- **Checks every mod.** Program files inside a mod are named and never installed. Every mod is scanned by your own
  antivirus (Microsoft Defender or whichever you use) before it is kept, and a mod it flags is refused. A download
  that differs from the known official one is pointed out.
- **Fixes.** Applies and removes the 4 GB (large address aware) patch and DXVK, and notices when Steam's Verify
  integrity has undone them.
- **Diagnoses.** Checks the install, the documents folder and the known community fixes, and suggests next steps.
- **Backs up saves**, keeping the newest backups.

**What it does not do**
- It does not include or download any game files or mods. It never sends anything over the internet.
- It does not change the game's own files except the two fixes, each behind a plan and a backup.

**Requirements**
- Windows 10 or 11, 64-bit. Nothing else to install (no .NET needed).
- Dragon Age: Origins (any edition). *Tested on a real Steam Ultimate Edition install. GOG and EA App installs are
  detected but have not yet been tried on a real machine; reports are welcome.* (Update this line if those runs
  happen before upload.)

**Installation**
Unzip anywhere and run `DAORework.exe`. Create a profile in the header: it finds your install and documents folder,
checks them, and lists next steps. `dao.exe` is the same tool on the command line. Your profiles, library and backups
live in `%LOCALAPPDATA%\DAORework`.

**Uninstall**
Roll back or deactivate the mods you activated through it (the Mods tab), remove the fixes (the Fixes tab), then
delete the folder you unzipped and `%LOCALAPPDATA%\DAORework`.

**Windows SmartScreen**
The executables are not code-signed, so Windows may say it protected your PC: **More info**, then **Run anyway**. Each
release has a build attestation (`gh attestation verify`) and a `.sha256` file to check the download against:
`Get-FileHash DAORework-0.3.0-win-x64.zip` in PowerShell, compared with `DAORework-0.3.0-win-x64.zip.sha256`.

**Source and support**
Free and source-available under the PolyForm Noncommercial License 1.0.0 (nobody may sell it): https://github.com/arastella625/DAORework. Bugs and questions go to the GitHub
issues; the **Export support bundle** button on the Diagnostics tab makes a zip with your user name removed.

**How it is built**
*(Short version of [docs/HOW_IT_IS_BUILT.md](../HOW_IT_IS_BUILT.md), in your voice; link to the full page.)*
I write C# for a living, and I design and direct DAORework. An AI coding assistant (Anthropic's Claude) writes much of
the code; I set the standards every change must pass and make the calls that touch your files and your safety. Every
change goes through automated checks, including a test that interrupts the program at every write to prove your
folders are never left half-changed, and I test each release on my own install. With BioWare closed, keeping this game
easy to set up for everyone matters more than ever. The full story, the research on AI and programming, and the water
question: [How it is built](https://github.com/arastella625/DAORework/blob/main/docs/HOW_IT_IS_BUILT.md).

**Credits**
DXVK (zlib license), Qwinn's Ultimate DAO Fixpack and Extra Dog Slot are only recognized by the catalog, never
included. Third-party libraries are listed in `THIRD-PARTY-LICENSES.md` inside the release zip.

## Nexus settings to choose on upload day

- **Category:** Utilities (or Modders' resources and tutorials, whichever fits Nexus's current list).
- **Generative AI tag:** choose from Nexus's guidelines as they stand on upload day; the code is written with AI
  assistance, so say so.
- **Permissions:** the PolyForm Noncommercial License allows reuse, changes and sharing for noncommercial purposes
  only; set Nexus's permission options to match ([nexus/permissions.md](nexus/permissions.md)).
- **Files:** upload the same `DAORework-0.3.0-win-x64.zip` as the GitHub release, and link the release for its
  `.sha256`.
- **Images:** the screenshots in `docs/screenshots/` (from the automated tests), or your own from a real run, which
  show real mod names.
