# Roadmap and Project Plan

This is the **strict path**. Phases run in order. A phase starts only when the
previous phase's **exit gate** is met in full. There is no partial credit and no
"we'll fix it in the next phase". Every task is a small PR that meets the
[Definition of Done](ENGINEERING_STANDARDS.md#8-definition-of-done).

Task IDs are stable: reference them in branch names and commits (`refactor/P0-07-cli-commands`).

Pull request numbers below (#1 to #160) refer to the project's development history before it moved to its public
repository on 2026-10-31. That history is kept privately; every decision it made is in `docs/adr`.

---

## Phase 0: Foundation hardening (no new features)

**Status: complete.** Closed on 2026-09-29 with tag `v0.0.1` (PRs #1 to #65). The exit gate
below is met: see the *Phase 0 re-run* in [CURRENT_STATE_AUDIT.md](CURRENT_STATE_AUDIT.md).

**Goal:** make the existing code conform to the SDD so that everything after it is
built on solid ground. User-visible behavior stays the same, apart from the listed bug fixes.

| ID | Task | Closes audit items |
|---|---|---|
| P0-01 | Decide Open Decisions D-2 (name) and D-3 (`Dao` prefix). Mark ADRs 0002–0006 *Accepted*. | D8 |
| P0-02 | Fix the red test (reference a code constant). Introduce `DiagnosticCodes`. | C1 |
| P0-03 | Retarget to .NET 10. Add `Directory.Packages.props`, `.editorconfig`, `TreatWarningsAsErrors`, analyzers and StyleCop. Fix every resulting warning. | D1, D3, D6 |
| P0-04 | Add a CI workflow: format check, build, test and coverage gate. Add the PR template and the file-size check script. | D1 |
| P0-05 | Split the test project into `Core/Application/Infrastructure/Architecture/Cli` test projects. Add architecture tests for the dependency rules. *(They are expected to fail until P0-06/07 land; mark them `Skip` with the task ID.)* | D2 |
| P0-06 | Introduce `GamePaths`, `ByteSize`, `ProfileName` value objects, `Result<T>`/`Error`, and `JsonDefaults`. Remove the duplication. | B1, B2, B3, C7 |
| P0-07 | Move I/O out of Core and Application: add `IFileSystem` (System.IO.Abstractions), `TimeProvider`, `IProcessLauncher` and `IFileHasher`. Make models immutable. | A2, A3, D4 |
| P0-08 | Split `DaoDiagnosticsService` into `RunDiagnostics` + one `IDiagnosticRule` per check, each with tests. | A2 |
| P0-09 | Split the mod services into the `InspectModFolders` and `BuildModInventory` use cases. Surface read errors instead of swallowing them. | C3 |
| P0-10 | Turn `DaoLauncherService` into the `LaunchGame` use case. Remove the auto-save side effect. Launch arguments go into `ArgumentList`. Resolve the runtime via keyed services. | C5, C8 |
| P0-11 | Move `init` logic into the `CreateProfile` and related profile use cases. Each layer gets its own `DependencyInjection.cs`, bound through `DaoReworkOptions`. | A4, A5 |
| P0-12 | Rewrite the CLI on Spectre.Console.Cli: one command class per verb, renderers, and exit codes. `Program.cs` ≤ 40 lines. Wire up `profiles list`. | A1, A6, B4, C2, C4 |
| P0-13 | Persistence hardening: add `schemaVersion`, atomic writes, collision-free report names, and a v0 → v1 migrator for existing profiles. | C6, D5 |
| P0-14 | Fix the README (encoding, commands) and add `CHANGELOG.md`. Re-run the audit and record the results. | D7 |
| P0-15 | Contributor tooling: a dev container matching CI (SDK from `global.json`, PowerShell 7) and Dependabot for NuGet and Actions. *The dev container was removed on 2026-10-06: development happens on Windows, where the verify script reaches the game install.* | — |

**Exit gate:**
- [x] Every audit item is closed. The audit document is re-run and shows zero open High/Blocker items.
- [x] Architecture tests are enabled (no `Skip`) and green.
- [x] Core and Application coverage is at or above 80%, and CI is green on `main`. *(98.9% and 94.1%)*
- [x] No file > 300 lines anywhere in `src/`. *(largest: 130 lines)*
- [x] The CLI does everything it did before, verified manually against a real install. *(`scripts/verify-real-install.ps1` on every code task since P0-10)*

---

## Phase 1: Companion v1 (read-only tooling)

**Goal:** a genuinely useful tool that never modifies game or user files.

- P1-00 The GUI shell ([ADR-0010](adr/0010-gui-avalonia-in-phase-1.md)): `DAORework.App` on Avalonia with a profile page (folder pickers for install, executable and documents), a diagnostics page, a mods page and a launch action, all over the existing use cases. `DAORework.App.Tests` renders screens headlessly; screenshots go on the PR. Later Phase 1 features are surfaced in the GUI as they land.
- P1-01 Real install detection: Steam `libraryfolders.vdf` (any library drive), GOG registry, EA App registry, and a manual path. One `IInstallLocator` per source.
- P1-02 Detection of the documents path, including OneDrive-redirected Documents folders.
- P1-03 A full diagnostic rule set: DirectX/VC++ prerequisites, the 4 GB large-address-aware flag state (reported only), resolution/config sanity, save-folder health, and DLC presence.
- P1-04 Support bundle export: diagnostics + inventory + logs, with path redaction, as one zip.
- P1-05 Save-game backup (copy **out** of the saves folder only) with retention settings.
- P1-06 Version 0.1.0 release: a self-contained single-file publish of the App (and the CLI) in GitHub Releases.
- P1-07 Known-items catalog and detection rules ([ADR-0009](adr/0009-third-party-mods-and-tools.md) tier 1): the large-address-aware flag, DXVK presence and version, Qwinn's Fixpack, and known-incompatible combinations, reported by diagnostic rules from a catalog kept as data in the repository.

**Status: closed on 2026-09-29 with one gate item carried forward.** All tasks are done (P1-00 to P1-07, PRs #70
to #77) and released as `v0.1.0`. The owner decided to start Phase 2 while the real GOG and EA App runs (and a Steam
run with launch) stay open as an issue ([#5](https://github.com/arastella625/DAORework/issues/5)), because nobody on the
project has those copies. The record is [testing/phase-1-matrix.md](testing/phase-1-matrix.md).

**Exit gate:**
- [ ] It works on Steam, GOG and EA App installs (manual test matrix recorded in `docs/testing/`). *(Steam verified on a real install; GOG and EA App covered by fake-registry tests only. Carried forward as [#5](https://github.com/arastella625/DAORework/issues/5).)*
- [x] No write operation targets any path outside the app-data folder. *(Audited: every writer is rooted in `DaoReworkOptions`; the saves and the install are only read.)*
- [x] Every Phase 1 feature is reachable in the GUI.
- [x] The headless screenshot tests are green.

---

## Phase 2: Mod manager (first write operations)

**Status: complete.** Closed on 2026-10-06 and released as `v0.2.0`. All tasks are done (P2-01 to P2-08, in
PRs #82 to #93) and the exit gate is met (PR #102). Verified on a real Steam install; Phase 1's GOG and EA App runs
are still carried as [#5](https://github.com/arastella625/DAORework/issues/5).

**Goal:** safe mod activation. This is the first code that writes to user folders, so it holds the highest standard.

- P2-01 An ADR for the mod storage model (a staging folder managed by DAORework, with activation into `override`/`AddIns`). *Done: [ADR-0012](adr/0012-mod-storage-model.md), a library in app-data, copied into DAORework's own subfolders.*
- P2-02 Conflict scanner: file-level overlaps and load-order implications.
- P2-03 `ModActivationPlan`, a pure domain object with a dry-run renderer.
- P2-04 Backup/snapshot service (the memento) with verified restore.
- P2-05 An executor that applies the plan with a journal, so an interrupted operation is resumable or reversible.
- P2-06 `mods activate/deactivate/rollback` commands, which always show the plan and require confirmation.
- P2-07 Import of `.dazip` and override archives (reading the archive manifest only).
- P2-08 Managed fixes ([ADR-0009](adr/0009-third-party-mods-and-tools.md) tier 2): apply the large-address-aware flag and install user-supplied permissively licensed fixes such as DXVK through the same plan, backup and rollback path. Fetching files needs its own ADR first. *Done: [ADR-0013](adr/0013-managed-fixes-in-the-install-folder.md); `fixes apply laa` patches the exe in place after a backup, `fixes apply dxvk <release>` installs DXVK from a release the user supplies, no elevation.*

**Exit gate:** a fault-injection test suite kills the process at every step of apply
and proves it recovers to either the before-state or the after-state, never a mix. *Met: [ADR-0014](adr/0014-fault-injection-exit-gate.md), `DAORework.FaultInjection.Tests` kills the real CLI before every one of its writes in eight mod and fix changes and recovers each kill both ways.*

---

## Phase 2.5: Launch-ready mod manager

**Status: release 0.3.0 prepared; the owner's real-install checks remain** (see the exit gate and
[docs/testing/phase-2.5-matrix.md](testing/phase-2.5-matrix.md)). Added on 2026-10-28, ahead of Phase 3.

**Goal:** the first public release (Nexus Mods). A player who already mods by hand can switch to DAORework without
reinstalling anything and without opening a command line, and has a clear reason to stay.

**Why this phase exists:** 0.2.0 works, but its mod management and fixes are CLI-only, and a hand-built `override` or
`AddIns` folder cannot be brought under management ([ADR-0012](adr/0012-mod-storage-model.md): a file DAORework did
not place is never moved or deleted). Those two gaps are the reasons a player would not switch from the manual
workflow or an existing manager. The project is not announced publicly until they are closed, because a first
impression on Nexus is hard to undo. Phase 3's format work does not help a player switch, so it waits.

Task IDs use the `P2.5-` prefix (branch `feat/P2.5-02-adopt-scan`). P3-04 moves here with its ID unchanged.

- P2.5-01 An ADR for adopting hand-installed mods. It amends ADR-0012's ownership rule with one explicit exception:
  the user may hand a hand-placed mod over to DAORework. The ADR decides how a hand-placed mod is recognized (a
  registered add-in UID, an override subfolder, catalog fingerprints, a matching library import), what happens to
  the original files (moved under DAORework's ownership, never deleted without a backup), and what stays unmanaged
  when nothing matches. *Done: [ADR-0015](adr/0015-adopting-hand-installed-mods.md); add-ins, override subfolders and
  loose override files can be adopted, override files are moved into DAORework's folder, and official DLC never is.*
- P2.5-02 Adopt scan (read-only): list every hand-placed mod in `override` and `AddIns`, and say which ones can be
  adopted and why the others cannot. Shown on the Mods page and as `mods adopt --dry-run`. *Done: `mods adopt
  --dry-run` and the Installed by hand list on the Mods tab, each candidate with its catalog entry, the library mod it
  already is (by SHA-256) and a Vortex warning; official DLC is never offered.*
- P2.5-03 Adopt apply: import the adopted mod into the library from the files on disk and move it into
  DAORework's layout and deployment record, through the plan, backup and journal path. `mods rollback` undoes an
  adoption like any other change. The fault-injection suite ([ADR-0014](adr/0014-fault-injection-exit-gate.md))
  gains adoption scenarios. *Done: `mods adopt <id>...`, with the plan, a backup that records moves, and rollback;
  the fault-injection suite kills it before every write when adopting an add-in where it is, an override folder,
  the loose files, and two folders sharing a file name, and recovers each kill both ways. The window's Adopt action
  came with P3-04.*
- P2.5-04 `.7z` and `.rar` import. A permissively licensed extraction library, checked under
  [ADR-0009](adr/0009-third-party-mods-and-tools.md) and justified in the PR. The same path-escape refusal as `.zip`,
  with malformed and malicious archive tests. *Done: `mods import` reads `.7z` and `.rar` through SharpCompress (MIT);
  a path leaving the mod's folder, a damaged or truncated archive, a file that is no archive, and a zip renamed `.7z`
  are refused with nothing added to the library, and a solid archive is read front to back once.*
- P2.5-10 GUI themes ([ADR-0016](adr/0016-gui-themes.md)): the theme tokens and four dark themes, *Witch of the
  Wilds* by default with *Korcari*, *The Fade* and *Blood and Shadow* to choose from on a new Settings page, saved in
  `preferences.json`. The existing pages move onto the tokens, and the App tests check every theme's tokens and AA
  contrast. Lands before P3-04 and the GUI half of P2.5-03, so their pages are built themed rather than reskinned.
  P2.5-02's *Installed by hand* panel came first and moves onto the tokens with the existing pages.
  *Done: the four themes (#119), the shared look from the mocks (#121), every page on it (#126), the saved choice
  (#128) and the Settings page's theme picker.*
- P2.5-11 Custom looks ([ADR-0017](adr/0017-custom-looks.md)): a background picture of the user's own, tinted and
  veiled into the theme with a veil the app keeps high enough for readable text, an accent color of their choice,
  and a look per launch profile with an app-wide default. Built on P2.5-10.
  *Done: the color rules (#130), the look store and each profile's saved choice (#132), picture import (#133), a look
  shown in the window (#135), the Settings page's look editor (#137), and the picture controls and each profile's
  look on the Profile page.*
- P3-04 *(moved from Phase 3)* Mod management in the GUI: the library with drag-and-drop import, activate and
  deactivate with the plan preview and confirmation, `status`, rollback to a chosen backup, and recovery after an
  interrupted change. *Done: the Library on the Mods tab lists the library with each mod's antivirus verdict,
  program files and known-download check, and imports from a file, a folder or a drop
  ([screenshot](screenshots/p3-04/01-library.png)); each mod is activated or deactivated from it, with the plan
  shown first and applied only on *Apply* ([screenshot](screenshots/p3-04/02-activate-plan.png)); the Changes panel
  shows what is active, rolls back to a chosen backup once confirmed, and finishes or rolls back an interrupted change
  ([screenshot](screenshots/p3-04/03-changes-rollback.png)); and each mod installed by hand is adopted from the
  window with its plan first ([screenshot](screenshots/p3-04/04-adopt-plan.png)).*
- P2.5-05 Conflict and load-order view: per file, which mods ship it and which copy wins (the P2-02 scanner); reorder
  the mod profile's list and see the plan change before applying. *Done: the Load order panel on the Mods tab lists the
  active mods with Up and Down, and each file more than one of them ships with whose copy wins; moving a mod plans the
  new order at once, its conflicts and plan shown before Apply, and an order that changes no file says it makes no
  difference ([screenshot](screenshots/p2.5-05/01-load-order.png)).*
- P2.5-06 Managed fixes and save backups in the GUI: apply and remove the 4 GB patch and DXVK (picking the downloaded
  release), fix status including "Steam's Verify integrity undid this", and save backup and list with retention.
  *Done: the Fixes tab applies and removes the 4 GB patch and DXVK (from a release picked on disk) with the plan shown
  first ([screenshot](screenshots/p2.5-06/01-fixes-plan.png)), and lists each applied fix as in effect or undone
  outside DAORework, naming Steam's Verify integrity ([screenshot](screenshots/p2.5-06/02-fixes-undone.png)); the
  Saves tab already backs up and lists saves, keeping the newest ones; fix changes are rolled back to a chosen backup
  once confirmed, and an interrupted one finished or rolled back, on the same tab
  ([screenshot](screenshots/p2.5-06/03-fixes-rollback.png)).*
- P2.5-07 First-run flow: detect, diagnose, then a short list of suggested next steps, each with its button
  (`DAO_LAA_DISABLED` offers *Apply 4 GB patch*, a missing DXVK offers *Install DXVK*, unmanaged mods offer *Adopt*).
  Report-only diagnostics stay report-only: nothing is applied without the plan and confirmation. *Done: the first
  profile created is diagnosed at once, and every diagnosis ends in Next steps on the Profile tab; each button switches
  to the tab its plan or list is on and opens it, and nothing changes until it is applied there
  ([screenshot](screenshots/p2.5-07/01-next-steps.png)).*
- P2.5-12 Mod safety ([ADR-0018](adr/0018-mod-safety.md)): every file of a mod classed as data or program at
  import, programs (by extension or an `MZ` header) named in the library and never deployed by a plan or moved by an
  adoption; every mod scanned through the Windows antimalware interface before it enters the library, a detection
  blocking the import and an unavailable scanner shown as *not scanned*; known official downloads recognized by
  their SHA-256 in the catalog, a differing copy of a known mod warned about; DAORework's own releases checkable
  (signed when the owner chooses, ADR-0018 §4).
  *Done: programs are named and never deployed, a renamed one caught by its header, and every import and adoption
  is scanned through AMSI, and a download is compared with the catalog's known downloads. Every release carries a
  checksum and a build provenance attestation; the release workflow signs the executables when the owner sets
  signing up ([setup](release/code-signing.md)), which she has chosen not to for now.*
- P2.5-08 Closed beta and the platform matrix: a small group of testers outside the project, including at least one
  GOG and one EA App install, run a written script (detect, adopt, import, activate, conflict, rollback, fixes).
  Results go in `docs/testing/phase-2.5-matrix.md`; issue [#5](https://github.com/arastella625/DAORework/issues/5)
  closes here.
- P2.5-09 Release 0.3.0 and the launch kit: the release, a Nexus page written by the owner, screenshots from the
  headless tests, and the generative-AI tag chosen by Nexus's guidelines as they stand on upload day. The README
  says plainly how the project is built, including the AI assistance.
  *Done: 0.3.0 released on 2026-10-31 on [GitHub](https://github.com/arastella625/DAORework/releases/tag/v0.3.0)
  and [Nexus Mods](https://www.nexusmods.com/dragonage/mods/7083), with its build attestation, and its checksum on
  the [website](https://daorework.dilly-world.net/).*

**Quality of life** *(added 2026-10-31, from the owner's own use after 0.3.0: adopting a real setup one mod at a time
is cumbersome)*. The command line already takes several mods at once (`mods adopt <id>...`, `mods activate
<id>...`) and the use cases plan them as one change; the window offers one mod at a time. These tasks bring the
window up to the command line. Each change still shows its plan first and is applied only on *Apply*.

- P2.5-13 Adopt several mods at once: each mod in *Installed by hand* gets a tick box, with *Select all*, and
  *Adopt selected* makes one plan for all of them, applied with one backup and undone by one rollback. Loose
  override files still need their name. *Done: tick boxes with Select all and Clear, and Adopt selected planning
  every ticked mod as one change ([screenshot](screenshots/p2.5-13/01-adopt-selected.png)).*
- P2.5-14 Activate and deactivate several Library mods at once: tick boxes in the Library, *Activate selected* and
  *Deactivate selected*, one plan for all of them. *Done: tick boxes with Select all and Clear, each choice planned
  as one change ([screenshot](screenshots/p2.5-14/01-activate-selected.png)).*
- P2.5-15 Import several downloads at once from *Import a mod* (dropping several already works), each scanned and
  checked as now, with one summary. *Done: the picker takes several files, imported in turn with one message.*
- P2.5-16 *Installed by hand* fills in by itself: the list is read when the Mods tab opens and after each change,
  instead of waiting for *Find mods installed by hand*. Finding still only reads. *Done: read when a profile is
  chosen and after every change; a profile without a documents folder shows an empty list, and the button is now
  Look again.*
- P2.5-17 The Mods tab in sections: *Library*, *Installed by hand* (with how many), *Load order*, *Changes* and
  *Folders*, instead of one long page with the folder figures first and *Installed by hand* last. Each plan shows
  in the section it was asked for. *Done: sections with Installed by hand counted, the Adopt next step opening it
  ([screenshot](screenshots/p2.5-17/01-sections.png)).*
- P2.5-18 Plans in the window name mods by their names, not their ids, open with a one-line summary (*copies 2
  files, moves 3*), and keep the file-by-file detail behind *Show details*. The command line's text is unchanged.
  *Done: in the Library, Installed by hand and Load order ([screenshot](screenshots/p2.5-18/01-plan-summary.png)).*
- P2.5-19 Each backup in *Changes* says what its change did (*Activated Eyes, Nails*, *Adopted 4 mods*), so the one
  to roll back to can be found without reading timestamps. *Done: on the Mods and Fixes tabs, worked out from what
  was active before and after each change, so older backups are labelled too
  ([screenshot](screenshots/p2.5-19/01-backup-labels.png)).*

**Mods that touch the same thing** *(added 2026-11-01, from the owner's question about combining
`chargenmorphcfg.xml`)*. Today a file two mods ship is reported once by name, and the last mod's copy wins. These
tasks combine what can safely be combined and say plainly what each remaining overlap means. The ones that need to
read the game's file formats wait for P3-01 (P3-07 to P3-09).

- P2.5-20 Combine `chargenmorphcfg.xml` from every active mod into one file DAORework owns, so every hair, head and
  colour mod shows in the character creator ([ADR-0025](adr/0025-combining-chargenmorphcfg.md)). *Done: combined in
  every plan (activate, deactivate, reorder, adopt), kept in the library under its hash, an unreadable copy named
  ([screenshot](screenshots/p2.5-20/01-combined.png)).*
- P2.5-21 Say what a shared file means, by its kind: textures and models (*one look wins*), `.gda` tables (*one
  mod's table changes are lost unless it extends the table (M2DA); look for a compatibility patch*), scripts (`.ncs`:
  *one mod's behaviour is lost*), talk tables (`.tlk`), archives (`.erf`: *their contents are not compared yet*), and
  `chargenmorphcfg.xml` (*combined*). In the plan's warnings, the Load order's shared files and the diagnostics.
  *Done in plans and the Load order (`SharedFileMeaning`); the diagnostics follow with P2.5-22, which brings the
  install folder's override into them.*
- P2.5-22 The install folder's override: read `<install>/packages/core/override`, where older guides put mods and
  which the game loads too. List what is there under *Installed by hand* as *in the install folder*, and include it
  in the shared-file warnings. Adopting from there needs ADR-0015 amended first.
- P2.5-23 Add-ins against each other: show the active add-ins with their `Priority` from `AddIns.xml`, and warn when
  two add-ins ship a file with the same name. How the game ranks add-ins is checked on the owner's install first;
  DAORework changes no priority until it is.

**Not in this phase:** Nexus "Mod Manager Download" (`nxm://`) links and any other network access (SDD §12 and
ADR-0009 need an ADR first); hard links for large texture packs (ADR-0012 leaves that to its own ADR); any format
reader.

**Exit gate:**
- [x] Every `mods`, `fixes` and `saves` capability is reachable in the GUI. Normal use needs no command line.
  *(Library, activation, Changes, Load order, Installed by hand and Adopt on the Mods tab; the Fixes tab with its
  rollback and recovery; the Saves tab.)*
- [x] On the owner's real install, a hand-built mod setup (at least Qwinn's Ultimate DAO Fixpack, one other add-in
  and one multi-folder override mod) is adopted without reinstalling, and deactivating and rolling back leave it
  as it was. *(Steam, 2026-10-31, owner's report: [matrix](testing/phase-2.5-matrix.md).)*
- [x] The fault-injection suite covers adopt and is green. *(Four adoption scenarios beside Phase 2's eight.)*
- [ ] Steam, GOG and EA App each have a real run recorded in `docs/testing/phase-2.5-matrix.md`. No platform is
  carried forward. *Steam is recorded (2026-10-31). GOG and EA App are still open as
  [#5](https://github.com/arastella625/DAORework/issues/5): nobody on the project has either copy, so 0.3.0 says
  plainly that only Steam has been run on a real install.*
- [ ] At least one beta tester who is not on the project completes adopt, import, activate and rollback in the GUI
  without the docs or help from the owner. *The owner decided on 2026-10-31 to release 0.3.0 without one, since
  nobody outside the project is available yet; the first public users take that role, and their reports are
  tracked as issues.*
- [x] The headless screenshot tests are green and cover every new page. *(Library, activation plan, Changes,
  adoption plan, load order, Fixes and its rollback, next steps.)*
- [x] On the owner's Windows machine, a zip holding the EICAR test file (made on the spot, never committed) is refused
  at import, and a mod with an `.exe` inside imports with the program named and is never deployed. *(2026-10-31,
  owner's report.)*

---

## Phase 3: Toolkit (file formats and GUI)

**Goal:** understand the game's data through clean-room readers and give the tooling a GUI.

- P3-01 The `DAORework.Formats` library (BCL only). Readers for ERF v2.0/v2.2, GFF v4.0 and TLK are built from public community specifications, which are cited in `docs/formats/`. DAOTools (MIT) may be ported with attribution; xoreos (GPL-3) supplies specifications and observed behavior only ([ADR-0009](adr/0009-third-party-mods-and-tools.md) tier 3). Each reader gets fuzz tests.
- P3-02 A data-browser CLI: list archive contents and dump GFF as JSON (for local inspection only; never redistributed).
- P3-03 Toolkit views in the GUI: archive browser and GFF viewer over the P3-01/P3-02 use cases. *(The GUI shell itself moved to P1-00; ADR-0010 decided D-4.)*
- *P3-04 moved to Phase 2.5.*
- P3-05 A music player for the user's own files *(the owner's wish, 2026-10-29; not format work, scheduled here so it
  comes after 0.3.0)*: a folder the user chooses, played in random order, with an on/off switch and a volume, off by
  default and paused while the game runs. Turning it on the first time offers *Import music you own*: a folder
  picker, played in place and never copied, and a soundtrack Steam installed under `steamapps\music\` is offered
  when one is found (the path is checked on a real install first). DAORework ships no music: the game's soundtrack
  is never bundled (LEGAL_BOUNDARIES). Playing the music inside the game's own audio packages would need a
  clean-room reader and an ADR that settles the EULA question, and is not part of this task: that ADR is
  [ADR-0019](adr/0019-the-games-own-music.md), proposed and waiting on the owner's decision and a format check. The audio library
  needs an ADR and the ADR-0009 license check first.

- P3-06 Knowing about new releases *(the owner's wish, 2026-10-31;
  [#19](https://github.com/arastella625/DAORework/issues/19))*: an opt-in *Check for updates*, off by default and
  asked once, that makes one anonymous request to GitHub's public releases API (no account, no key, no profile or
  path sent), compares versions, and links to the GitHub release and the Nexus page with how to check the download.
  It is DAORework's first network access, so an ADR comes first (SDD §12): what is sent, where and how often, the
  opt-in, and why GitHub rather than the Nexus API, which needs each player's own key. Installing an update itself
  (download, verify the SHA-256 and the build attestation, replace the running executables) is not part of this task
  and would need its own ADR and threat model.

- P3-07 Conflicts inside `.erf` archives, over the P3-01 reader: the resource names inside each mod's override
  archives, compared with loose files and with each other, so a shared resource is found however it is packed.
- P3-08 `.gda` tables row by row, over the P3-01 reader: when two mods ship the same table, which rows each
  changes. Combining tables whose changes do not overlap is its own ADR, with the M2DA mechanism as the first answer.
- P3-09 Talk table string ids two mods both use, over the P3-01 reader.

**Exit gate:** each reader round-trips every file in a real install without exceptions (verified by a local-only test that runs on the developer's machine against their own install; no game files are committed).

---

## Phase 4+: The remaster track

These phases are defined once [Open Decision D-1](SDD.md#13-open-decisions) is settled.
The planning inputs will be: the Phase 3 format coverage, a survey of existing
community work, and a scoped research spike (a model/texture viewer is the usual first step).
The reference point for any engine work is xoreos ([ADR-0009](adr/0009-third-party-mods-and-tools.md)):
if that path is taken, the project contributes there rather than starting a third engine.
This phase gets its own SDD addendum before any code is written.

---

## Working rhythm

- **One task = one branch = one PR.** Keep each one small enough to review in 15 minutes.
- **Verify before the PR.** A task is shown working (CLI transcript, GUI screenshots) and checked by the owner against a real install where that matters, before its PR is opened. See CONTRIBUTING.md.
- At the end of each phase: re-run the audit, update this roadmap, and tag a release.
- If the plan is wrong, change the plan in a `docs:` PR first, then change the code.
