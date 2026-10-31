# ADR-0018: Mod safety: programs flagged, an antivirus scan at import, known downloads and signed releases

- **Status:** Accepted
- **Date:** 2026-10-29

## Context
DAORework is aimed at players who mod by hand today, many of whom do not know what is inside a download. A mod for
Dragon Age: Origins is game data: `.erf`, `.gda`, `.dds`, `.uti`, `.ncs`, `.tlk` and the like, placed in `override`
or `AddIns`. The game never runs a program from those folders. A program or a script in a mod is therefore either a
helper tool the author bundled (a ReShade or DXVK `.dll`, an installer `.exe`) or something worse, and a re-upload of
a popular mod is an easy way to hand out malware.

DAORework already never runs anything from a mod, refuses archive paths that would leave the library (P2-07), and
only writes into the documents folder through a confirmed plan. It does not say what kind of files a mod holds, does
not check them with the user's antivirus, and cannot tell a mod's real download from a tampered copy. Its own
releases are unsigned, so Windows SmartScreen warns on first run, and that teaches players to click through warnings.

The release is planned for Nexus Mods (which scans uploads itself) and perhaps itch.io. The owner chose all four
protections below, and how each one acts, on 2026-10-29.

## Decision

### 1. Programs in a mod are flagged and never deployed
- At import, every file is classed as **data** or **program**. A program is a file whose extension is one Windows
  runs or loads (`.exe`, `.dll`, `.com`, `.scr`, `.cpl`, `.ocx`, `.sys`, `.msi`, `.msp`, `.bat`, `.cmd`, `.ps1`,
  `.psm1`, `.vbs`, `.vbe`, `.js`, `.jse`, `.wsf`, `.wsh`, `.hta`, `.jar`, `.lnk`, `.url`, `.reg`), or any file that
  starts with a Windows executable header (`MZ`), so a renamed `.exe` is caught too. The list lives in Core, in one
  place, with tests.
- A mod with programs is still imported (chosen by the owner). The library, `mods import` and the GUI say how many
  program files it holds and name them. **The planner never deploys a program file**: the plan lists them as not
  installed, and why. A DXVK `d3d9.dll` bundled in a mod is pointed at `fixes apply dxvk` instead.
- Adoption (ADR-0015) applies the same rule to hand-installed files: a program file found in `override` or `AddIns`
  is reported, and an adoption plan never moves it.

### 2. Every mod is scanned by the user's antivirus before it enters the library
- Before an import is committed, each extracted file is passed to the **Windows Antimalware Scan Interface (AMSI)**,
  which hands it to Microsoft Defender or whatever antivirus is registered. DAORework calls `amsi.dll` itself
  (P/Invoke, no new package) behind an `IMalwareScanner` port, with the Windows adapter in Infrastructure.
  Adoption scans the files it imports from disk the same way.
- **A detection blocks the import** (chosen by the owner): nothing enters the library, and the error names the file
  and the antivirus's verdict (`DAO_MOD_SAFETY_THREAT_FOUND`). There is no override in DAORework; a player who is sure
  it is a false positive deals with it in their antivirus.
- Program files are always scanned in full. A data file is scanned whole up to 256 MB; a larger one (a texture
  archive) is recorded as not scanned, because AMSI scans a buffer in memory. The extraction itself also writes every
  file to disk, where the antivirus's real-time protection sees it.
- When AMSI is unavailable (not Windows, AMSI disabled, or no antivirus answering), the import goes ahead and the mod
  is recorded and shown as **not scanned** (`DAO_MOD_SAFETY_SCAN_UNAVAILABLE`, a warning). Blocking would make the
  tool unusable on such a machine, and saying so is honest.
- DAORework itself sends nothing anywhere (SDD §12). What the antivirus does with a file, such as cloud lookups or
  sample submission, is its own setting.
- Wording is careful: a clean scan is shown as "No threats found by <antivirus>", never as "safe".

### 3. Known downloads are recognized
- The catalog (ADR-0011) gains a fingerprint kind **`DownloadSha256`**: the SHA-256 of an official download of the
  item, with the version and the page it came from. The two archive hashes `catalog/README.md` already records
  (Qwinn's Fixpack 3.5, Extra Dog Slot 1.41) become the first ones.
- At import, the download's SHA-256 is compared with the catalog. A match is shown as "matches the known download of
  <item> <version>". A download that the catalog identifies as an item (by its add-in UID or its file names) but whose
  hash matches none of that item's known downloads is shown as **"differs from the known downloads of <item>: get it
  from <source>"** (`DAO_MOD_SAFETY_DOWNLOAD_DIFFERS`, a warning, not a block: authors update their files).
- A new release of a mod is added to the catalog the same way as today: from its own download, with the hash in the
  pull request.

### 4. DAORework's own releases are signed and checksummed
- Each release's executables are **code-signed**, so SmartScreen and antivirus software can trust them and players
  are not taught to click through warnings. The certificate or signing service is chosen when the repository goes
  public: free open-source programs (SignPath Foundation, for example) need a public repository and an application,
  and a paid certificate or a cloud signing service needs the owner's identity checked. The release workflow signs in
  CI; no key is ever committed.
- *Decided 2026-10-31 by the owner:* **Azure Artifact Signing**, with the owner validated as an individual. It works
  while the repository is still private, costs a monthly fee instead of a yearly certificate, and needs no secret in
  GitHub: the release job signs in to Azure through OpenID Connect, and Azure accepts only the GitHub environment
  `release`. Every `v*` tag is signed; a tag without the signing setup stops before anything is published. Pull
  request builds stay unsigned. Setup: [docs/release/code-signing.md](../release/code-signing.md). Signing names the
  publisher at once, but SmartScreen also weighs reputation, which grows with downloads, so the first signed release
  may still be warned about.
- *Changed later on 2026-10-31 by the owner:* an individual's certificate carries the owner's verified legal name and
  may add where she lives, which she does not want on every download. **Signing is optional:** the release job signs
  only when all of the signing setup is in place, publishes unsigned when none of it is, and stops when only part of
  it is. Instead of a publisher name, every release carries a **build provenance attestation** (GitHub's, through
  Sigstore) for the zip and both executables: a signed record that these exact files were built by this repository's
  release workflow from a given commit, which anyone can check with `gh attestation verify`, and which names the
  repository, not a person. GitHub offers it free for public repositories only, so a release from the private
  repository goes without. Each release is also submitted to Microsoft's file analysis as its developer, which can
  help Defender and SmartScreen trust it. SmartScreen keeps warning on unsigned releases; the README and the Nexus
  page say why and how to check the download. Ways to sign later without the owner's name (a business of her own, or
  the Microsoft Store) need their own decision. SignPath Foundation is out: it requires an OSI-approved license, which
  [ADR-0020](0020-license-noncommercial.md) is not.
- Every release asset keeps its published `.sha256` (as today), and the release notes and the Nexus page say how to
  check it.

### Where the code goes
- **Core:** the program-file classification, the verdict types, and the planner rule that skips program files.
- **Application:** the `IMalwareScanner` port; scanning and classification in the import and adoption use cases.
- **Infrastructure:** the AMSI adapter, and the library importer, which scans the staged files before the rename that
  commits an import.
- **Persisted:** `mod.json` schema 3 records each file's class and the scan verdict at import (scanned clean, not
  scanned and why). Its migrator classes existing entries' files by reading them once from the library and records
  them as not scanned.

## Consequences
- A player sees, before anything is installed, whether a mod holds programs, whether their antivirus objected, and
  whether it is the download the catalog knows. Programs never reach the game's folders through DAORework.
- Imports take longer: every file is read once more for the scan. Texture packs over 256 MB per file are not scanned
  by DAORework.
- The AMSI adapter only runs on Windows; CI runs on Linux, so its tests use a fake scanner, and the owner's
  real-install checklist covers the adapter: a zip holding the standard EICAR test file, made on the spot and never
  committed, is refused at import.
- The catalog's `DownloadSha256` entries need keeping up to date as authors release new versions; a stale entry only
  ever produces a warning.
- Signing, if the owner turns it on, adds a monthly Azure fee and a one-time setup (identity validation, an app
  registration and the `release` environment); the workflow holds no secret. Until then, releases are unsigned,
  SmartScreen warns on first run, and the checksum and the build attestation are how a player checks a download.
- The work ships as separate PRs: this ADR; program classification and the planner rule; `mod.json` schema 3; the
  scanner port and the AMSI adapter in import and adoption; `DownloadSha256`; release signing.

## Alternatives considered
- *Block any mod with a program in it:* safest, but it rejects legitimate mods that bundle a helper, and players would
  install those by hand instead, which is worse.
- *Ask the player each time:* players who do not know what a `.dll` is click yes.
- *Let the player import a mod the antivirus flagged:* false positives happen, but a mod manager that installs a
  flagged file at a click is exactly what an attacker wants. The antivirus is where an exception belongs.
- *Upload hashes to VirusTotal or a similar service:* needs network access (SDD §12) and an account, and sends what
  the player installs to a third party.
- *An allowlist of data extensions instead of a list of program ones:* mods use many data formats, and an unknown one
  would be flagged for no reason. The `MZ` check catches the renamed programs an allowlist was meant to catch.
- *Rely on Nexus's own scan:* it covers Nexus uploads only, not files from elsewhere or a copy changed afterwards.
