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

**Status: closed on 2026-10-31 with release `v0.3.0` and two gate items carried forward.** Its goal, the first
public release, was met: P2.5-01 to P2.5-07, P2.5-09 to P2.5-12 and P3-04 are done. The GOG and EA App runs and an
outside beta tester (P2.5-08, [#70](https://github.com/arastella625/DAORework/issues/70) and
[#5](https://github.com/arastella625/DAORework/issues/5)) are carried into [Phase 2.6](#phase-26-after-launch), and so
is everything added after 0.3.0 (P2.5-13 to P2.5-38), with their IDs unchanged. Added on 2026-10-28, ahead of Phase 3.

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

*Carried forward (owner, 2026-11-03): the two open items stay open in Phase 2.6's exit gate, as Phase 1's
platform item was carried into Phase 2.*

---

## Phase 2.6: After launch

**Status: in progress.** Releases `v0.3.1` to `v0.5.0` came from this work (see [CHANGELOG.md](../CHANGELOG.md)). Open:
P2.5-23, P2.5-31's add-ins ([#116](https://github.com/arastella625/DAORework/issues/116)), the SonarCloud findings
(P2.5-37, P2.5-38), P2.6-09, and the two items carried from Phase 2.5's exit gate. Opened on 2026-11-03 for the work that had
been added to Phase 2.5 after 0.3.0.

**Scheduled on 2026-11-04 (P2.6-11):** the owner accepted ADR-0031, ADR-0033 and ADR-0034 and asked for work towards a
large next release without waiting for this phase's exit gate, whose open items need the owner or outside testers.
They are carried forward, as Phase 1's and Phase 2.5's were. Phases 2.8 and 2.7 and P3-10 and P3-11 start now, in that
order, after P2.5-31.

**Goal:** a player who switched to DAORework stays. What the owner and the first players find in real use is fixed
or smoothed: several mods at once, mods that touch the same files, a window organised around what a player does,
taking over from Vortex, and the code health SonarCloud reports.

**Task IDs:** the tasks added to Phase 2.5 after 0.3.0 move here with their IDs unchanged (P2.5-13 to P2.5-38),
as P3-04 moved into Phase 2.5. New tasks use the `P2.6-` prefix (branch `fix/P2.6-01-documents-anywhere`).

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
  in the shared-file warnings. Adopting from there needs ADR-0015 amended first. *Done: `InstallOverrideRule` in the
  diagnostics; *Found in your game* lists each folder and the loose files, tagged *in your game folder* as VOICE.md
  words it, with a filter chip and no *Manage*; and activation plans warn about a file of the same name
  ([screenshot](screenshots/p2.5-22/01-game-folder.png)).*
- P2.5-23 Add-ins against each other: show the active add-ins with their `Priority` from `AddIns.xml`, and warn when
  two add-ins ship a file with the same name. How the game ranks add-ins is checked on the owner's install first;
  DAORework changes no priority until it is.

**A workflow-first window** *(added 2026-11-01, from the owner's review of a clickable mock: six tabs instead of 11
places, plain words, mods told apart by how the game loads them, and a richer on-theme look;
[ADR-0026](adr/0026-workflow-first-layout.md))*. Each task is its own PR with screenshots; the command line is
unchanged.

- P2.5-24 The words: [VOICE.md](VOICE.md) applied to every view and message (*Turn on*, *Manage*, *Undo*, *Found in
  your game*, *Preview*), ids, codes and paths moved behind *Details*, success messages that fade, plurals and local
  times. No layout change. *Done: the new words on every button, tab and message, ids gone from rows and codes put
  after their messages, *1 file* and *2 files*, success messages that fade after a few seconds (what stands, such as
  a count or a verdict, stays), times like *Today, 18:05*, and one footer line in place of the repeated reassurance.
  A few notes the command line shares keep their wording until the CLI gets its own.*
- P2.5-25 Home, the first tab: ready to play or not, the readiness strip for add-ins, override mods and combined
  files, the last change with *Undo*, and at most three suggestions (P2.5-07's Next steps move here).
  *Done: Home opens first, says whether the game is ready (or offers *Check the game*), shows add-ins, override mods
  and combined files with what is on and any overlap, the newest change from Mods or Fixes with *Undo* (asked on its
  History, confirmed there), and *Worth a look* ([screenshot](screenshots/p2.5-25/01-home.png)).*
- P2.5-26 Mods as one list: an on/off switch per mod, the three cards that filter it, *Found in your game* in the
  same list with *Manage*, *Arrange* for the load order, and Combined files listing each shared file as combined,
  one wins, or needs a patch (P2.5-21's meanings). Agreed with the P2.5-21 to P2.5-23 work before it starts.
  *Done: Library, Found in your game and Arrange are one page under the three kind cards, with All, On, Off and Found
  chips, *Arrange* as a toggle, and Combined files listing each merged file and each file one mod wins
  ([screenshot](screenshots/p2.5-26/01-mods-one-page.png)); the Mods tab keeps History and Folders as sections until
  P2.5-27. The on/off switches come with P2.5-28's templates; P2.5-22's install-folder mods and P2.5-23's add-in
  priority land in this page as a tag and a filter (owner, 2026-11-01).*
- P2.5-27 History and Settings: every change from Mods and Fixes in one timeline with *Undo*; the profile's folders,
  the look and Troubleshooting (diagnostics and the support bundle) move into Settings.
  *Done: the window has six tabs and no tab inside another; History holds the mod and fix changes with *Undo to here*
  ([screenshot](screenshots/p2.5-27/01-history.png)); Settings holds the look, then *Game folders* (the Profile tab)
  and *Troubleshooting* (diagnostics, the support bundle and the mod folders), closed until opened, and Home's buttons
  open them ([screenshot](screenshots/p2.5-27/02-settings-troubleshooting.png)). The changes are two lists, Mods then
  Fixes; one timeline mixing both waits for a reason to merge them.*
- P2.5-28 The gilded-tome style: button, frame, switch and seal templates from tokens, Kenney's *Fantasy UI Borders*
  (CC0) as tinted masks, and the screenshots for the README, the Nexus page and the website retaken.
  *Done: gold-edged buttons with Cinzel labels and inner rules, cards with a double rule and filigree in every corner,
  gem on/off switches, readiness seals, a sunburst behind *Ready to play*, and Kenney's border (CC0) around Home's cards,
  all drawn from the theme's tokens, with no ornaments in Korcari ([Home](screenshots/p2.5-28/01-home.png),
  [Mods](screenshots/p2.5-28/02-mods-switches.png)); the README's and the website's screenshots are retaken, the website saying the pictures
  show the next release. The Nexus page's are retaken with that release, so they show what can be downloaded.*

**Taking over from Vortex** *(added 2026-11-03, [ADR-0028](adr/0028-taking-over-from-vortex.md))*.

- P2.5-29 Taking over from Vortex: an ADR and a real check. Players who installed with Vortex switch without finding
  their downloads again, and Vortex is never left half-managing the game.
  *Done: [ADR-0028](adr/0028-taking-over-from-vortex.md) is accepted. The owner chose a guided handover (DAORework
  copies, the player purges in Vortex, DAORework turns the copies on) for mods Vortex still manages.
  `scripts/check-vortex.ps1` finds the game, its documents folder and Vortex's staging folder on any drive. On the
  owner's PC it found 70 Vortex mods left behind: the documents folder had moved to `D:` and the staging folder was
  empty, so Vortex can no longer reach them. The live handover's add-in details wait on a PC where Vortex has deployed
  a `.dazip`.*
- P2.5-30 Read Vortex's record: *Found in your game* names the mods Vortex placed, tagged *Placed by Vortex*, in
  place of one warning per folder; the ones Vortex left behind can be managed.
  *Done: each Vortex mod is the files its record lists that are still in `override`, wherever they sit, named without
  its Nexus id and version, and its files leave the folder and loose-file entries. A mod Vortex can no longer reach
  offers *Manage*; one it still manages does not, and `mods adopt` refuses it (`DAO_MOD_ADOPT_VORTEX_MANAGES`). A
  record that can't be read keeps today's warning. `mods adopt --dry-run` lists them as `vortex:<name>`
  ([screenshot](screenshots/p2.5-30/01-vortex-mods.png)).*
- P2.5-31 The handover (ADR-0028 §3 and §4): the *Move from Vortex* card on the Mods page and `mods vortex copy`;
  the copies, a check that the purge is done, one preview to turn them on in Vortex's order, and Home's suggestion
  when Vortex deploys again. *Done for override mods ([screenshot](screenshots/p2.5-31/01-card.png)), with the DLC
  repair and a fault-injection test for the copy; add-ins Vortex placed wait on a real `.dazip` deployment (ADR-0028 §6).
  Run only on simulated records so far.*

**SonarCloud's first findings** *(added 2026-11-02, from the first analyses after
[ADR-0027](adr/0027-sonarcloud-analysis-in-ci.md); 113 open findings and a red, report-only quality gate)*. Each task
is one issue and one PR. The analyzers and ENGINEERING_STANDARDS stay the authority: a finding is fixed when it is
right and marked in SonarCloud, with the reason, when it is not. Making the gate blocking is a later, separate
decision.

- P2.5-32 Pin the workflow dependencies: `azure/login` and `Azure/artifact-signing-action` in `release.yml` to full
  commit SHAs with the version in a comment, as the Nexus upload action already is (`githubactions:S7637`), and
  `dotnet-sonarscanner` to an exact version in `ci.yml` (ADR-0027's follow-up). *Done
  ([#64](https://github.com/arastella625/DAORework/pull/64)): `azure/login` at v3.1.0's commit, the signing action at
  v2.0.0's, and the scanner at 12.0.0.*
- P2.5-33 Open zip entries asynchronously (`OpenAsync`) in `FixSourceReader`, `ZipSaveBackupStore` and
  `ZipSupportBundleWriter` (`csharpsquid:S6966`). A refactor: no behavior changes. *Done
  ([#67](https://github.com/arastella625/DAORework/pull/67)), every test unchanged.*
- P2.5-34 `scripts/verify-real-install.ps1`: put `$null` on the left of its two comparisons (`powershelldre:S8641`),
  so a comparison against an array cannot filter instead of compare. *Done
  ([#68](https://github.com/arastella625/DAORework/pull/68)); both checks pass on the owner's install (2026-11-03).*
- P2.5-35 Source-generated code where it costs nothing: `GeneratedRegex` in `DiagnosticCodesTests` (`SYSLIB1045`).
  The AMSI calls in `AmsiNativeMethods` keep `DllImport`, with `SYSLIB1054` suppressed and the reason given:
  `LibraryImport` needs `AllowUnsafeBlocks`, which Infrastructure keeps off, and without trimming or AOT it gains
  nothing (owner, 2026-11-03). It is reconsidered if the app is ever trimmed or compiled ahead of time.
  *Done ([#79](https://github.com/arastella625/DAORework/pull/79)).*
- P2.5-36 The Nexus cover page (`docs/release/nexus/cover.html`) gets a `<title>` and `lang`. *Done
  ([#66](https://github.com/arastella625/DAORework/pull/66)); the cover's screenshot is byte-for-byte the same.*
- P2.5-37 The collection-expression hints (about 85, mostly `IDE0305` and `IDE0028`, set to *suggestion* in
  `.editorconfig`): either raise them to *warning* and apply them in one refactor PR, or keep them as suggestions and
  stop SonarCloud reporting them. *The owner chose on 2026-11-04 to keep them as suggestions; what is left is to stop
  SonarCloud reporting them.*
- P2.5-38 Mark the findings that are wrong, in SonarCloud, with the reason: `LookColor`'s exact comparisons
  (`csharpsquid:S1244`; `max` is always exactly one of r, g and b) and `GrainTexture`'s seeded `Random`
  (`csharpsquid:S2245`; seeded on purpose so the screenshots stay the same). No code changes.

**Found on the owner's PC** *(added 2026-11-03, from `scripts/verify-real-install.ps1` run for P2.5-34)*.

- P2.6-01 Find the documents folder wherever it is ([#74](https://github.com/arastella625/DAORework/issues/74)):
  `detect` looks only in the Documents folder Windows reports and its OneDrive copy, and misses the owner's, which
  is on `D:`. It also reads the folders the profiles name and searches the drives, as `scripts/check-vortex.ps1`
  does, and the user can still name it. The verify script finds it the same way.
  *Done ([#101](https://github.com/arastella625/DAORework/pull/101), [ADR-0032](adr/0032-finding-the-documents-folder.md)).*
- P2.6-02 The tests pass on Windows ([#75](https://github.com/arastella625/DAORework/issues/75)): Cli, Infrastructure
  and App tests fail on the owner's PC while CI (Linux) is green on the same commit. First, find which tests fail
  and why. *Done ([#95](https://github.com/arastella625/DAORework/pull/95)).*

**Review setup** *(added 2026-11-03, from a `/ponytail-review` of PRs #76 and #78)*.

- P2.6-03 The PR guard holds up ([#80](https://github.com/arastella625/DAORework/issues/80)): `pr-guard.yml` labels a
  PR from its full file list, not the first 100 files, and clears `needs-maintainer-review` when a maintainer releases
  the PR. CODEOWNERS also owns `Directory.Build.props` and every `.csproj`, since `VersionOverride` lets a project
  change a package version. *Done ([#83](https://github.com/arastella625/DAORework/pull/83)).*
- P2.6-04 CI starts again ([#84](https://github.com/arastella625/DAORework/issues/84)): from 2026-11-03 every run
  ended in `startup_failure` after a change to the repository's Actions settings, fixed there by the owner. The
  SHA-pin setting was not the cause, but `ci.yml` and `release.yml` now pin GitHub's own actions to commit SHAs too.
  *Done ([#83](https://github.com/arastella625/DAORework/pull/83), [#85](https://github.com/arastella625/DAORework/pull/85)).*

**Releasing on Nexus** *(added 2026-11-04: Nexus holds every upload for a manual check, as it did 0.3.1 and 0.4.0)*.

- P2.6-06 Say it up front ([#86](https://github.com/arastella625/DAORework/issues/86)): the README, the website and
  the Nexus description say a new version may be held on Nexus for a day or two, and that the same zip is on GitHub
  straight away. `docs/release/nexus-upload.md` says what to do after each upload, including reporting a false
  positive to the antivirus engine Nexus names. *Done ([#89](https://github.com/arastella625/DAORework/pull/89)).*
- P2.6-07 Fewer flags, quicker reviews ([#87](https://github.com/arastella625/DAORework/issues/87)): the release no
  longer compresses the single-file executables, which antivirus heuristics treat like packed programs, and each
  release run writes the filled-in review request to Nexus into its summary. A VirusTotal scan in CI waits until
  these two have been tried on a few releases. *Done ([#90](https://github.com/arastella625/DAORework/pull/90)).*
- P2.6-08 The Nexus page's text from the repository ([#91](https://github.com/arastella625/DAORework/issues/91),
  [ADR-0030](adr/0030-nexus-page-text-from-the-repository.md)): a workflow sends `docs/release/nexus/summary.txt`
  and `description.bbcode` to Nexus when they change on `main`, so the owner no longer pastes them. The gallery, the
  name, permissions and the category stay manual. The description gains P2.6-06's note about Nexus's manual check.
  *Done ([#93](https://github.com/arastella625/DAORework/pull/93), [#94](https://github.com/arastella625/DAORework/pull/94)).*
- P2.6-10 The real-install check knows Steam starts the game ([#99](https://github.com/arastella625/DAORework/issues/99),
  [ADR-0022](adr/0022-launching-steam-installs-through-steam.md)): `verify-real-install.ps1` still wanted a process
  id, so a Steam install failed two launch checks on the owner's run of 2026-11-03 although the game started. The
  launch check accepts either success message, and the official launcher check is skipped on a Steam install, where
  the launch target does not apply. GOG and EA App installs keep it. *Done ([#100](https://github.com/arastella625/DAORework/pull/100)).*
- P2.6-11 Schedule the next release: this roadmap brought up to date, and ADR-0031, ADR-0033 and ADR-0034 accepted, as
  the owner decided on 2026-11-04.
- P2.6-12 Release 0.5.0: the version, the changelog's section, the audit re-run and this roadmap. The tag is the
  owner's, after the hand checks. Phases 2.6, 2.7 and 2.8 stay open: 0.5.0 carries what is done of each.

**A real library** *(added 2026-11-04, from the owner's first run of 0.5.0 on about 70 mods moved over from Vortex: a
preview of 63 mods was one paragraph of names and hundreds of warnings, with what stopped it last. Screen tests had two
made-up mods, which is how it got through.)*

- P2.6-13 A preview reads in one screen: what stops it first, each cause once with what to do, *Leave out* for the mods
  behind it, warnings grouped by the mods involved, and files that mean nothing to the game left out of the warnings.
  `RealScaleLibrary` (70 mods, 8,000 files) guards it, and window changes are checked against it from now on
  ([REVIEWING.md](REVIEWING.md)). *Done ([blocked](screenshots/p2.6-13/01-blocked.png), [ready](screenshots/p2.6-13/02-ready.png)).*

- P2.6-14 Only mod files reach the game ([ADR-0036](adr/0036-only-mod-files-reach-the-game.md)): Vortex's
  markers, Mac leftovers and documents stay in the library and are never deployed, and a folder of nothing else isn't
  offered as a mod.

- P2.6-15 The library reads clean: names read as words, `loose` reads *Loose files*, a mod with nothing for the game
  isn't listed, and *Combined files* counts shared files, not only merged ones.

- P2.6-16 *Combined files* and History at real scale: past 30 files, one row for each set of mods, not each file;
  History counts the mods on instead of naming 66. `RealScaleScreensTests` renders every main screen.

- P2.6-18 Copies of a mod ([#146](https://github.com/arastella625/DAORework/issues/146)): add-ins with one UID and
  mods with the same files are noticed, each says so, and *Select all* picks one. Removing a copy from the library is
  a separate decision: the library has no way to remove a mod yet.

- P2.6-19 Arrange moves a mod far in one step ([#144](https://github.com/arastella625/DAORework/issues/144)): *To top*
  and *To bottom*, each with one preview. Dragging a mod to a place in the middle waits for the design pass's answer.

- P2.6-20 A preview warns only when it matters: a file in the override folder that DAORework doesn't manage is compared
  with the mod's, and one with the same content isn't mentioned; only mods the change adds are. On the owner's library
  all 551 files warned about were the same as Vortex's copies. ADR-0012 is amended.

- P2.6-21 *Manage* shows who wins: mods with different files of the same name are listed by set, each with *Swap*,
  which plans again in the new order (ADR-0015's "the user can reorder before confirming"). On the owner's library
  all 46 such files were in different folders, so Vortex's record can't settle them.

- P2.6-22 A big change says how far it has got: *Apply* and *Undo* show a banner across the window, which locks until they
  end. The owner's 7 GB change took two minutes with the window looking idle, and starting another reported that the
  first `did not finish`.

- P2.6-23 *Manage* clears leftovers: a mod already on no longer stops *Manage*. The copies found in the game are deleted
  with a backup, as ADR-0015 now says. Turning mods on before managing them left the owner with two copies of each.

**One path through the window** *(added 2026-11-05, from the owner's first real run; [WORKFLOWS.md](WORKFLOWS.md))*.

- P2.6-24 Map the workflows: the five journeys, where each breaks, and [ADR-0037](adr/0037-a-mod-is-one-thing.md).
- P2.6-25 *Turn on* takes over what is in the game: identical files are moved, not copied.
- P2.6-26 One list: a mod in the Library and the game shows once. *In part:* the two panels stay, and copies of a mod are still separate rows until a copy can be removed.
- P2.6-27 One next step, worked out from where the player is.
- P2.6-28 Today's small fixes: the copies note names the other copy, and *Finish* shows the progress banner.
- P2.6-29 One-line rows that expand in place ([ADR-0038](adr/0038-rows-a-sheet-and-drag.md)).
- P2.6-30 Search and sort, and *Select all* ticks only what is shown.
- P2.6-31 Previews in one sheet under the list, replacing the scroll-into-view fixes.
- P2.6-32 A quieter preview, and *Worth a look* no longer repeats the next step.
- P2.6-34 From a review of the stack: a search no longer changes Home's counts, and an unfinished fix is a next step.
- P2.6-35 Drag in *Arrange*, with one preview for the whole move.
- P2.6-36 First steps: a short path on the next-step card, gone once a mod is on.
- P2.6-37 After the run: WORKFLOWS.md brought up to date, the new words in VOICE.md, and draft release notes.
- P2.6-33 Conflict marks on rows: *wins*, *loses*, *wins and loses*. Not on `.erf` contents, where the winner isn't known.
- P2.6-38 Decide how to look before adding, keep one copy and combine more files ([ADR-0039](adr/0039-look-first-and-keep-one-copy.md), [ADR-0040](adr/0040-combining-more-than-one-file.md)).
- P2.6-39 Look before adding: a download that looks already held offers *Manage it* or *Add anyway* first.
- P2.6-40 Keep one copy: *Keep this one* removes a mod's other copies from the Library, with a preview.
- P2.6-43 *Manage* gets past a mod it meets twice: a leftover of a mod that is on, or one mod found in two places, is one mod.
- P2.6-51 From the review of #156 to #169: an emptied name no longer stops *Take them over*, and only the left button drags.
- P2.6-52 Readmes aren't mods: on the owner's library 26 of 88 entries held only readmes, PDFs, `.DS_Store` or `Thumbs.db`, each a copy of what its full mod holds. *Take them over* no longer makes them, and those there count as part of the mod of their name.
- P2.6-60 Removed copies go to the Recycle Bin, as the owner chose from the OpenTS3 design's "nothing is ever lost" (ADR-0039).
- P2.6-41 Count the differing files by kind on a real library, which sets the order of P3-01, P3-08 and combining (ADR-0040). *Done: of 50 names that differ, 44 are textures, 3 scripts, 2 materials and 1 a cutscene, with no tables. Choosing who wins for each file comes first, and tables wait.*
- P2.6-42 Keep one of each: every set of copies cleared from one preview, parts of mods included, offered as the next step, with each pick changeable and nothing lost unless the player picks ([ADR-0039](adr/0039-look-first-and-keep-one-copy.md)).

**When something goes wrong** *(added 2026-11-06, the owner's focus on less technical players)*. A problem should come
with a diagnosis and a way forward, not leave a player stuck. The owner saw a raw error stop a batch of 54 mods.

- P2.6-53 Skip what fails: a batch goes on without the mod that fails and says which and why, and no exception's text reaches the window.
- P2.6-54 Rank the problems: shared files are counted and ordered by what can go wrong, a file inside an archive says what to do, and `alistair_main.dlg` reads as Alistair's conversation.
- P2.6-55 Mods that contain each other: a mod whose files are all in a bigger one says so and offers to keep that one, looking inside `.erf` files ([ADR-0041](adr/0041-mods-inside-mods.md)).
- P2.6-56 Find the problem mod: half the mods go off each round, the player plays and answers, until one is left, about 8 rounds for 137 mods. Every step is a preview with Undo, and everything goes back ([ADR-0042](adr/0042-finding-the-problem-mod.md)).
- P2.6-57 What changed: each *Play* records the exact mods, and the window says what's new since the last game with other mods, with *Go back to those mods* ([ADR-0043](adr/0043-last-known-good.md)).

**Feeling quick** *(added 2026-11-06, the owner's wish, from clearing copies on a messy real library)*. Buttons don't
always highlight, and during a wait it isn't clear whether to wait or press again. Measured on `RealScaleLibrary` (138 mods,
made-up files, no real disk, so a real library is slower):

| What | Time on the window's thread | Note |
| --- | --- | --- |
| Reading the Library again | about 0.8 s | 9,730 changes to the list on screen, where about 138 would do, and 278 next-step changes |
| Reading *Found in your game* again | about 0.5 s | |
| One search keystroke | about 0.35 s | Nearly all of it is laying out every row |
| Planning one mod | about 0.25 s | |

After every change the window reads all of it again in turn, so it is stuck for at least 1.5 seconds. Highlights can't
change while that thread is busy. A button is also disabled while its command runs, so it doesn't highlight then either,
and no view shows `IsBusy`, so nothing says work is going on. Only three things in the window run off its thread.

Each stage is worth having alone, and they go in this order, so the wait is explained before it is shortened:

- P2.6-44 Measure: limits on the work a reload does, checked on `RealScaleLibrary` (counts, since times vary from one
  machine to another), and in the log how long each step took and how long the window was held, so a real run can be
  compared. The owner's messy library is the real check.
- P2.6-45 Always say what's happening: the window's footer says what it is working on, whichever tab is open. A disabled
  button looks disabled, and hover shows on every button in every theme.
- P2.6-46 Stop repeating work: the list and the next step update once for a reload, not once for each mod. *Done: 9,730
  list changes became 1. *Found in your game* went from about 0.5 s to 0.13 s and planning one mod from 0.25 s to 0.12 s, but
  reading the Library only from about 0.8 s to 0.6 s and search not at all. Laying out every row is the real cost, so
  P2.6-49 matters more than this list first suggested, and should come next.*
- P2.6-47 Read once: a change refreshes the panels from one reading of the Library, instead of each reading it again.
- P2.6-48 Keep the window free: reading, comparing and planning run off the window's thread.
- P2.6-49 Long lists: only the rows on screen are built, and a row is kept when its mod hasn't changed, so search and
  scrolling don't depend on how many mods there are. *Done for your mods' list: a search keystroke went from about 0.35 s to
  0.03 s, and reading an unchanged Library from about 0.6 s to 0.17 s. The owner tried both on a real library: scrolling is fine, though
  the scrollbar doesn't take the mouse for a moment, and the Tab key works.
  *Found in your game* and *Arrange* still build every row: *Arrange*'s drag needs each row to exist, so it needs its own look.*
- P2.6-50 Less disk work: don't hash a file again when its size and time haven't changed, and let a swap move files instead
  of copying and backing them up (ADR-0039's known cost). A saved hash can be wrong, so this needs its own ADR first.

**Done when:** with 140 mods, a press shows a response within 0.1 s, the window's thread is never held for over 0.2 s, and
a search keystroke takes under 0.05 s, on `RealScaleLibrary` and on the owner's library.

**Keeping the tests honest on Windows** *(added 2026-11-05, from P2.6-02)*.

- P2.6-09 Run the tests on Windows in CI: `ci.yml` runs them on Linux only, which is how P2.6-02's failures reached
  `main` unnoticed. A second job runs `dotnet test` on `windows-latest` for every PR and push to `main`, and
  `.github/rulesets/main.json` makes it a required check. The Linux job stays as it is.

**Not in this phase:** the same as Phase 2.5's.

**Exit gate:**
- [ ] P2.5-22, P2.5-23 and P2.5-31 are done.
- [ ] The SonarCloud tasks (P2.5-32 to P2.5-38) are done, and the quality gate is green. It stays report-only
  unless a separate decision makes it blocking.
- [ ] P2.6-01 to P2.6-04 and P2.6-06 to P2.6-09 are done, and `scripts/verify-real-install.ps1` reports no FAIL on
  the owner's PC.
- [ ] Carried from Phase 2.5: Steam, GOG and EA App each have a real run in `docs/testing/phase-2.5-matrix.md`, and
  at least one beta tester from outside the project completes adopt, import, activate and rollback in the GUI
  (P2.5-08, [#70](https://github.com/arastella625/DAORework/issues/70)). The owner may carry these forward again.
- [ ] Released as a tagged version, with the audit re-run and this roadmap updated.

---

## Phase 2.8: Mods that run inside the game

**Status: in progress.** P2.8-01 to P2.8-03 are in `v0.5.0`, so these mods are recognised and never half-installed;
installing them is P2.8-04 onward. [ADR-0033](adr/0033-mods-that-run-inside-the-game.md) is accepted. Added
2026-11-03, after comparing DAORework with the tools on Nexus.
The owner chose the same day to run it before Phase 2.7, so it comes first here; it keeps its number because Phase 2.7's
task IDs were already published. Issues are opened when the phase is scheduled.

**Goal:** a player can install a mod that puts a DLL in `bin_ship`, such as DAO Lib, with the same scan, preview, record
and undo as any other mod, and says yes to the program first. A mod that cannot be installed says so.

**Task IDs:** `P2.8-` (branch `feat/P2.8-02-game-program-mods`).

- P2.8-01 Decide it: ADR-0033 and this phase.
- P2.8-02 Never half-installed: a mod for `bin_ship`, in either of ADR-0033's two shapes, is recognised, nothing from it
  is copied into `override`, and its preview says what will happen. *Done: `ModKind.GameProgram`, and until P2.8-04
  turning one on is blocked, naming it ([screenshot](screenshots/p2.8-02/01-preview.png)).*
- P2.8-03 See what is there: `DAO_INSTALL_PROGRAM_FOUND` reports a DLL in `bin_ship` that is neither the game's nor in
  the record, or that no longer has the recorded hash. *Done (`BinShipProgramsRule`); the GOG and EA copies' own DLLs are
  still to be compared.*
- P2.8-04 Plan and apply: `PlanGameProgramMod` and `ApplyGameProgramMod` on the fix path's record, journal and backups,
  with ADR-0033's refusals. A refactor PR comes first if a shared piece is fix-only.
- P2.8-05 Say yes to the program: the tick box in the preview, *Turn on selected* leaving these mods out, and
  `--allow-programs <modId>` on the command line.
- P2.8-06 What a mod needs: the catalog's `needs` list and its preview warning, with entries for DAO Lib, the controller
  mod and Luma, and a conflict entry linking DAO Lib's `dinput8.chain.dll` page.
- P2.8-07 Fault injection: scenarios for turning a `GameProgram` on and off
  ([ADR-0014](adr/0014-fault-injection-exit-gate.md)).
- P2.8-08 Say so: the README, the website, the Nexus description, the plan warning, `docs/VOICE.md` and ADR-0018 stop
  promising that programs are never installed, in the same release as P2.8-05.

**Not in this phase:** mods that ship game data as well as programs; renaming a DLL; taking over a DLL placed by hand;
these mods on Linux; a replacement `daorigins.exe`, which is never in scope.

**Exit gate:**
- [ ] P2.8-01 to P2.8-08 are done.
- [ ] On the owner's real install, DAO Lib and Luma are each turned on, load in the game, are turned off and undone, and
  the folder ends as it started, apart from files the mod wrote itself.
- [ ] A tester with Steam's protected executable runs ADR-0033's *Not yet seen* items, recorded in `docs/testing/`.
  Anything not run there is not claimed as supported.

---

## Phase 2.7: Steam on Linux

**Status: in progress.** P2.7-01 to P2.7-06 are in `v0.5.0`, which has no Linux download yet (P2.7-08). Scheduled on
2026-11-04, after Phase 2.8. [ADR-0031](adr/0031-steam-on-linux-through-proton.md) is
accepted. Added 2026-11-04, after a trial build ran in Ubuntu under WSL.
Issues are opened when the phase is scheduled.

**Goal:** a Linux player with the Steam copy of the game, running through Proton, can find their game, manage mods,
back up saves and press Play, from a `linux-x64` download. Windows players see no change.

**Task IDs:** `P2.7-` (branch `fix/P2.7-02-known-folders`).

- P2.7-01 Decide it: ADR-0031 and this phase.
- P2.7-02 No relative paths: a known folder that does not exist yet keeps its name, and an empty one is never joined
  into a path. Today a bare home folder puts profiles and logs in the folder DAORework was started from, and `init`
  saves the relative path `BioWare/Dragon Age`. *Done: `DoNotVerify` on the three folders a home has, and the readers
  skip an empty one.*
- P2.7-03 Find Steam on Linux: `SteamLibraries` reads the native, Flatpak and Snap Steam folders and every library
  they list, wherever it is mounted, and `SteamLibraryLocator` uses it. The player can still name the folder. *Done;
  the Flatpak and Snap paths are the ones ADR-0031 takes from community reports.*
- P2.7-04 Find the documents folder in the Proton prefix: `IProtonPrefixLocator`, `DocumentsFolderSource.ProtonPrefix`
  and `DAO_PROTON_PREFIX_MISSING`. Diagnostics and `mods status` say plainly when the prefix, and the mods in it, have
  gone. *Done; `IHostPlatform`, named in P2.7-06, comes in here.*
- P2.7-05 Play on Linux: a Steam copy starts through `xdg-open`; any other copy fails with `DAO_LAUNCH_NOT_SUPPORTED`
  and points to the player's launcher. Launch messages stop saying *Windows*. *Done; the real `xdg-open` waits for
  P2.7-09.*
- P2.7-06 Advice that fits Linux: `IHostPlatform`, and DXVK is no longer suggested there. A DXVK already in the game
  folder is still recognised and can be removed. *Done: `DetectKnownItems` leaves it out of what is missing, so neither
  diagnostics nor Home suggest it.*
- P2.7-07 The window opens: `Program.Main` names the missing library in one line instead of a stack trace, and a CI
  job starts the published window under `xvfb`.
- P2.7-08 A Linux download: `DAORework-<version>-linux-x64.tar.gz` from `publish-release.ps1` and `release.yml`, with a
  checksum and a build attestation, on GitHub only.
- P2.7-09 A real run: `docs/testing/phase-2.7-matrix.md`, on a Linux machine with real Steam and Proton. It settles
  what ADR-0031 takes from community reports: the prefix's folder names and letter case, the Flatpak and Snap paths,
  and what Proton uses for Direct3D 9 in this game.
- P2.7-10 Say so: the README, the website and the requirements (`libice6`, `libsm6`). The Nexus page is a separate
  decision after P2.7-09.

**Not in this phase:** GOG or EA copies under Heroic, Lutris, Bottles or Wine beyond naming the folders by hand;
Flatpak, AppImage or distribution packages; macOS; malware scanning on Linux; changing any Proton setting.

**Exit gate:**
- [ ] P2.7-01 to P2.7-10 are done.
- [ ] A Linux player with real Steam and Proton completes detect, import, activate, rollback, a save backup and Play
  in the window, recorded in `docs/testing/phase-2.7-matrix.md`. Anything not run there is not claimed as supported.
- [ ] The Windows verify script still reports no FAIL on the owner's PC.
- [ ] Released as a tagged version, with the audit re-run and this roadmap updated.

---

## Phase 3: Toolkit (file formats and GUI)

**Goal:** understand the game's data through clean-room readers and give the tooling a GUI.

- P3-01 The `DAORework.Formats` library (BCL only). Readers for ERF v2.0/v2.2, GFF v4.0 and TLK are built from public community specifications, which are cited in `docs/formats/`. DAOTools (MIT) may be ported with attribution; xoreos (GPL-3) supplies specifications and observed behavior only ([ADR-0009](adr/0009-third-party-mods-and-tools.md) tier 3). Each reader gets fuzz tests.
  *ERF done (`docs/formats/erf.md`); GFF and TLK to come. The exit gate's run over a real install is the owner's:
  `DAOREWORK_GAME_DIR` turns the test on.*
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
  [#19](https://github.com/arastella625/DAORework/issues/19); [ADR-0035](adr/0035-opt-in-update-check.md), proposed)*: an opt-in *Check for updates*, off by default and
  asked once, that makes one anonymous request to GitHub's public releases API (no account, no key, no profile or
  path sent), compares versions, and links to the GitHub release and the Nexus page with how to check the download.
  It is DAORework's first network access, so an ADR comes first (SDD §12): what is sent, where and how often, the
  opt-in, and why GitHub rather than the Nexus API, which needs each player's own key. Installing an update itself
  (download, verify the SHA-256 and the build attestation, replace the running executables) is not part of this task
  and would need its own ADR and threat model.

- P3-07 Conflicts inside `.erf` archives, over the P3-01 reader: the resource names inside each mod's archives, in
  override and in add-ins, compared with loose override files and with each other, so a shared resource is found however
  it is packed. An add-in's own `manifest.xml` doesn't count.
  *Done: on the Combined files card ([screenshot](screenshots/p3-07/01-packed.png)). No mod is said to win: which copy
  the game uses isn't known.*
- P3-08 `.gda` tables row by row, over the P3-01 reader: when two mods ship the same table, which rows each
  changes. Combining tables whose changes do not overlap is its own ADR, with the M2DA mechanism as the first answer.
- P3-09 Talk table string ids two mods both use, over the P3-01 reader.

**New downloads** *(the owner's wish, 2026-11-03: a built-in browser so downloads are dealt with automatically;
[ADR-0034](adr/0034-picking-up-new-downloads.md), accepted, meets the wish from the player's own browser)*. Not format
work; scheduled here as P3-05 and P3-06 are. None of it goes online except what P3-12 decides.

- P3-10 Picking up new downloads: an opt-in, asked once, off by default. When the window comes back into focus,
  DAORework looks in the player's Downloads folder, wherever it is, or a folder they name. Finished archives that look
  like a Dragon Age mod are offered on a *New downloads* card, with a suggestion on Home. *Add* is the existing import,
  so nothing is added without a click, and the download itself is only ever read. `mods downloads` lists the same files.
  Needs `/security-review`: archive readers now run on files nobody chose. *Done
  ([screenshot](screenshots/p3-10/01-new-downloads.png)); run only on test folders so far.*
- P3-11 Opening a mod's page: *Open the mod's page* wherever the window names a known item's source, in the player's
  default browser, `https` links only. `ShellUriLauncher`'s error stops mentioning Steam for a web page. *Done: on the
  Library's and *Found in your game*'s rows, through `OpenModPage`; a test holds every catalog source to `https`
  ([screenshot](screenshots/p3-11/01-open-page.png)).*
- P3-12 Asking Nexus about `nxm://` links: the owner asks Nexus how a registered app should handle *Mod Manager
  Download*, for free and premium accounts. A go-ahead needs its own ADR before any code (SDD §12): the opt-in, what is
  sent, registering the app, and sharing the link type with Vortex.

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
