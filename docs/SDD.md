# Software Design Document — DAORework

| | |
|---|---|
| **Status** | Draft v0.4. D-2 and D-3 are decided ([ADR-0007](adr/0007-project-and-type-naming.md)); license and third-party policy decided ([ADR-0008](adr/0008-project-license-mit.md), [ADR-0009](adr/0009-third-party-mods-and-tools.md)); D-4 decided ([ADR-0010](adr/0010-gui-avalonia-in-phase-1.md)); mod storage decided ([ADR-0012](adr/0012-mod-storage-model.md)); managed fixes decided ([ADR-0013](adr/0013-managed-fixes-in-the-install-folder.md)); the Phase 2 exit gate decided ([ADR-0014](adr/0014-fault-injection-exit-gate.md)); D-1 remains [open](#13-open-decisions) |
| **Owner** | Amber Rastella |
| **Last updated** | 2026-09-29 (v0.4: GUI in Phase 1) |
| **Companion docs** | [ENGINEERING_STANDARDS.md](ENGINEERING_STANDARDS.md) (the rules) · [ROADMAP.md](ROADMAP.md) (the path) · [CURRENT_STATE_AUDIT.md](CURRENT_STATE_AUDIT.md) (where we are) · [adr/](adr/) (why) · [LEGAL_BOUNDARIES.md](LEGAL_BOUNDARIES.md) |

> **How to use this document.** The SDD says *what* the system is and *how* it is
> shaped. The Standards say *how every line is written*. The Roadmap says *in what
> order* work happens and what "done" means for each phase. When they disagree, fix
> the documents first, then write the code. When you make a design choice that is
> not already covered here, record it as an ADR before merging.

---

## 1. Purpose and vision

DAORework is an unofficial, community project to modernize the experience of
playing **Dragon Age: Origins** on current hardware, using a copy of the game that
the user owns and has installed.

It is built in layers of ambition. Each layer must be useful on its own and must
make the next one possible:

1. **Companion** — detect installs, manage launch profiles, diagnose problems, launch the game.
2. **Mod manager** — inventory, conflict detection, safe activate/deactivate with backup and rollback.
3. **Toolkit** — clean-room readers for the game's file formats (ERF, GFF, TLK, ...), plus an asset/data browser.
4. **Remaster track** — the long-term goal. Its exact form is [Open Decision D-1](#13-open-decisions).

**The guiding principle:** never build layer *N+1* on an unfinished layer *N*.
The "remaster" succeeds or fails on the quality of layers 1 to 3.

## 2. Scope

### In scope
- Windows desktop (Win10 and Win11, x64) first. Keep the Core and Application layers portable so Linux/Proton support is possible later.
- The Steam, GOG and EA App distributions, plus manual installs.
- Reading from the user's install and documents folders. Writing only to DAORework's own app-data folder and, in Phase 2+, to the user's mod folders, always behind backup and rollback.

### Out of scope, permanently (see [LEGAL_BOUNDARIES.md](LEGAL_BOUNDARIES.md))
- Shipping, redistributing or bundling any BioWare/EA asset, script, text or code.
- Decompiling or copying proprietary implementation code.
- DRM circumvention and ownership-check bypass.
- Bundling third-party mods without permission.
- Copying code from GPL projects (xoreos among them). Their documentation and behavior may inform clean-room work ([ADR-0009](adr/0009-third-party-mods-and-tools.md)).

## 3. Quality attributes (in priority order)

When two attributes conflict, the higher one wins.

1. **Safety of user data.** Never lose a save, a mod or a setting. Every destructive operation is plan → confirm → backup → apply → verify, and can be rolled back.
2. **Correctness and honesty.** Report what actually happened. A failure is never reported as success, and an unreadable file is never counted as "no duplicates".
3. **Maintainability.** Small, single-purpose units behind clear seams. A new feature adds files rather than growing existing ones.
4. **Testability.** Every rule in Core and Application can be tested without touching the real disk, registry, clock or processes.
5. **Diagnosability.** Structured logs, stable diagnostic codes, and machine-readable reports.
6. **Performance.** It must be fast enough on a mod folder with 50k files. Correctness comes first.

## 4. Architecture

### 4.1 Style: Clean Architecture (ports and adapters) with feature slices

```
                ┌───────────────────────────────────────────┐
                │ Presentation  (DAORework.Cli, later .App) │  parse input, render output
                └──────────────────────┬────────────────────┘
                                       │ calls use cases
                ┌──────────────────────▼────────────────────┐
                │ Application  (use cases + ports)          │  orchestration, no I/O
                └──────────────────────┬────────────────────┘
                                       │ depends on
                ┌──────────────────────▼────────────────────┐
                │ Core  (domain model + rules)              │  pure, no I/O, no framework
                └───────────────────────────────────────────┘
                ┌───────────────────────────────────────────┐
                │ Infrastructure  (adapters)                │  implements Application ports:
                │   file system, registry, JSON, processes  │  disk, registry, process, JSON
                └───────────────────────────────────────────┘
```

### 4.2 The dependency rule (enforced by architecture tests, see §10)

| Project | May reference | Must NOT reference |
|---|---|---|
| `DAORework.Core` | BCL only | anything else; `System.IO.File`/`Directory`, `Microsoft.Win32`, `System.Diagnostics.Process` |
| `DAORework.Application` | Core, `Microsoft.Extensions.*.Abstractions`, `System.IO.Abstractions` | Infrastructure, Cli, `System.IO.File`/`Directory` static APIs, `Microsoft.Win32` |
| `DAORework.Infrastructure` | Core, Application | Cli |
| `DAORework.Cli` | Application, Infrastructure (composition root only), Core | — |
| `DAORework.App` (Phase 1, [ADR-0010](adr/0010-gui-avalonia-in-phase-1.md)) | Application, Infrastructure (composition root only), Core | Cli |
| `DAORework.Formats` (Phase 3) | BCL only | everything else, so it stays reusable and testable in isolation |

### 4.3 Feature-sliced folders

Each layer is organized **by feature**, not by technical kind. This way a feature's
code lives together, and no folder grows without bound.

```
src/
  DAORework.Core/
    Installations/     Profiles/     Diagnostics/     Mods/     Launching/     Common/ (Result, Error, ByteSize)
  DAORework.Application/
    Installations/  DetectInstallations.cs            IInstallLocator.cs
    Profiles/       CreateProfile.cs  UpdateProfile.cs  GetProfile.cs  ListProfiles.cs  IProfileRepository.cs
    Diagnostics/    RunDiagnostics.cs  IDiagnosticRule.cs  Rules/*.cs
    Mods/           InspectModFolders.cs  BuildModInventory.cs  IFileHasher.cs
    Launching/      LaunchGame.cs  IGameRuntime.cs  IGameRuntimeResolver.cs
    Common/         IReportWriter.cs  GamePaths.cs
    DependencyInjection.cs
  DAORework.Infrastructure/
    Installations/  SteamLocator.cs  GogLocator.cs  EaAppLocator.cs  WellKnownPathLocator.cs
    Persistence/    JsonProfileRepository.cs  JsonReportWriter.cs  JsonDefaults.cs  Migrations/
    Launching/      ProcessGameRuntime.cs  ExperimentalRuntime.cs
    Mods/           Sha256FileHasher.cs
    DependencyInjection.cs
  DAORework.Cli/
    Program.cs                (≤ 40 lines: build host, run app)
    Commands/Profiles/        ProfileShowCommand.cs  ProfileSetPathCommand.cs  ...
    Commands/Mods/            ModsInspectCommand.cs  ModsInventoryCommand.cs
    Rendering/                DiagnosticReportRenderer.cs  ProfileRenderer.cs
  DAORework.App/              (Phase 1, Avalonia)
    App.axaml  Program.cs     composition root
    Views/  ViewModels/       one view + view model per feature page, over the use cases
tests/
  DAORework.Core.Tests/  DAORework.Application.Tests/  DAORework.Infrastructure.Tests/
  DAORework.Architecture.Tests/  DAORework.Cli.Tests/  DAORework.App.Tests/
```

## 5. Design patterns: where each one is used, and why

These patterns are **prescribed**. Use a pattern only where it is listed here or in
an ADR. A pattern that solves no problem here counts as complexity.

| Pattern | Where | Problem it solves |
|---|---|---|
| **Use Case / Command Handler** (one class, one public `ExecuteAsync`) | Every Application operation (`LaunchGame`, `CreateProfile`, ...) | Prevents "god services" that gain a method per feature. Each use case has exactly the dependencies it needs. |
| **Ports and Adapters** (Repository, Gateway) | `IProfileRepository`, `IInstallLocator`, `IReportWriter`, `IFileHasher`, `IProcessLauncher` | Keeps I/O out of the domain, so the logic can be tested. |
| **Strategy** | `IDiagnosticRule` (one class per check), `IInstallLocator` (one per store), `IGameRuntime` (one per runtime mode; the original game's chooses Steam or a direct start, [ADR-0022](adr/0022-launching-steam-installs-through-steam.md)), `IFingerprintMatcher` (one per known-items fingerprint kind found on disk, [ADR-0011](adr/0011-known-items-catalog.md); a `DownloadSha256` hash is compared at import by `KnownDownloads` instead, [ADR-0018](adr/0018-mod-safety.md)) | New checks, stores and runtimes are **added** as new files. Existing code is not edited (Open/Closed). |
| **Composite / Aggregator** | `RunDiagnostics` runs every registered `IDiagnosticRule`, `DetectInstallations` merges every `IInstallLocator`, and `DetectKnownItems` runs the matchers over every catalog entry | One entry point with independent, testable parts. |
| **Factory via keyed DI** | `IGameRuntimeResolver`, backed by .NET keyed services | Chooses a strategy by enum without a hand-written `switch` over the container. |
| **Result object** (`Result<T>` / `Error`) | Return type of use cases and domain operations for *expected* failures | Makes failure explicit in the signature. Exceptions are reserved for bugs and truly exceptional I/O. See ADR-0004. |
| **Value Object** | `GamePaths`, `ProfileName`, `ByteSize`, `DiagnosticCode`, `RelativeModPath` | Puts validation and formatting in one place (fixes audit items B1, B2, C7). |
| **Options** | `DaoReworkOptions` (app-data root, ...) bound via `IOptions<T>` | Replaces the constructor `string` paths wired by hand in DI. |
| **Plan → Apply (dry-run) + Memento** | Phase 2 mod activation: `ModActivationPlan` → `IModActivationExecutor`, with the backup acting as the memento | Destructive file operations are previewable, confirmable and reversible (Quality Attribute #1). |
| **Command (CLI)** | One class per CLI verb (Spectre.Console.Cli, see ADR-0005) | Removes the `Program.cs` god file (audit item A1). |
| **Schema versioning + Migrator chain** | All persisted JSON | Lets old profiles on user machines keep loading after the model changes. |

**Rejected on purpose:** MediatR (a commercial license since 2025, and it adds
indirection with no benefit at this size), a generic `IRepository<T>` (it leaks
persistence concerns), AutoMapper (hides mapping bugs), and an event bus (no
consumer needs one yet). Revisit only via an ADR.

## 6. Domain model (Core)

Rules for every Core type:
- **Immutable by default:** `sealed record` or `init`-only properties. Collections are exposed as `IReadOnlyList<T>`/`IReadOnlyDictionary<,>`.
- **Constructed valid:** use a factory such as `ProfileName.Create(string) → Result<ProfileName>`. No invalid instance can exist.
- **No I/O, no `DateTime.Now`, no `Environment.*`.** Time comes from `TimeProvider`, which is injected at the Application layer.

Key aggregates:

| Type | Responsibility |
|---|---|
| `Installation` | Where a game install lives, which distribution it is, and which executables were *found at detection time* (data, not live `File.Exists`). |
| `RuntimeProfile` | A named launch configuration: paths, launch target, runtime mode, arguments, the active mod profile, and `SchemaVersion`. Changes produce a new instance (`profile with { ... }`). |
| `GamePaths` | **The single source of truth** for DAO layout conventions: `bin_ship\daorigins.exe`, `DAOriginsLauncher.exe`, `packages\core\override`, `AddIns`, the default documents root. |
| `DiagnosticReport` / `DiagnosticIssue` / `DiagnosticCode` | Results of the checks. Codes are constants in one catalog class and never change once released (see §8). |
| `ModInventory` / `ModFile` / `ModFolderSummary` | Snapshots of the mod folders. Duplicate detection is a domain rule here. |
| `LaunchResult` | The outcome of a launch request. |
| `LibraryMod` / `ModLayout` / `ConflictScanner` | A mod as held in DAORework's library, where each of its files is deployed, and what an ordered set of mods deploys once overlaps are resolved: one copy per override file name, the later mod winning; collisions outside override block the plan ([ADR-0012](adr/0012-mod-storage-model.md), P2-02). Program files (`ProgramFiles`, [ADR-0018](adr/0018-mod-safety.md)) are left out before any of that and listed in the report, so no plan ever deploys one: a file whose recorded `ModFileKind` is `Program` (its extension or an `MZ` header at import) or that is named like one. |
| `DeploymentRecord` / `ModActivationPlan` / `ModActivationPlanner` | What DAORework has placed in a documents folder, and the plan that turns it into a requested list of mods: copies, replacements and deletes, each with the hash the target must still have, the `AddIns.xml` entries to add or remove, and the blockers (a file changed since it was placed, a file DAORework did not place, an add-in registered by hand). `ModActivationPlanText` renders the dry run (P2-03). |
| `DeploymentBackup` / `BackupEntry` / `DocumentsKey` | The memento (P2-04): what every path a plan touches held before it ran. For a file that existed, a saved copy and its hash; for a path that was empty, the hash the plan writes there, so a restore removes only that. Kept by `IDeploymentBackupStore`; used by `BackUpDeployment` and `RestoreDeployment`. A file a plan moves (`PlanAction.Move`, ADR-0015) is not copied: its entry records the move (`MovedFrom`, the target, the SHA-256), and restoring moves it back, or copies it from the library by its hash when it is gone (`MovedFileRestorer`); a different file where it was is never overwritten. Schema 2. |
| `ApplyJournal` / `ApplyProgress` / `RecoveryChoice` | The executor's journal (P2-05): the plan's steps, its backup, and the records for after and before, written before the first change; finished steps are appended as they finish. `ApplyModPlan` runs a plan through `IDeploymentWriter`, the only code that writes mod files into the documents folder, with every step safe to repeat; a move (ADR-0015) is a rename inside the documents folder (`DocumentsFileMover`). Schema 2. `RecoverModPlan` settles an unfinished apply: Finish runs the remaining steps, RollBack restores the backup. |
| `LibraryEntry` / `ArchiveClassifier` / `AddInManifest` / `ModIds` / `ModPath` | The mod library (P2-07): a mod's `mod.json` (the mod, its source's name and hash, when it was imported, and, from schema 2, its `origin`: a download, or adopted from the documents folder with the path it came from, [ADR-0015](adr/0015-adopting-hand-installed-mods.md); from schema 3, each file's `ModFileKind` and the mod's `MalwareScan` verdict, [ADR-0018](adr/0018-mod-safety.md), with older entries classified once by `LibraryFileClassifier` when read; `LibraryImporter` passes the staged files through `IMalwareScanner` (`AmsiMalwareScanner`, the Windows antimalware scan) before the rename that commits an import, and a detection deletes them); what an archive holds (a `.dazip`, a Nexus zip with one `.dazip` in it, or override files); the UID and name from a `Manifest.xml`; the library id; and the one rule that keeps an archive path inside its folder. `IModLibrary` (`FolderModLibrary`) imports a `.dazip`, `.zip`, folder or, through SharpCompress (`CompressedModSource`, P2.5-04), a `.7z` or `.rar` into `library/<modId>/` and writes `library/<modId>.mod.json` last. |
| `ModChange` / `DocumentsSnapshot` | Mod activation (P2-06): a change to the active list (activate appends at the end, so a new mod wins; deactivate removes; set replaces) and what the planner needs from the documents folder, read by `IDocumentsScanner` (`DocumentsScanner`): the hashes of the paths in play, override files outside `DAORework/`, and the UIDs in `AddIns.xml`. `PlanModChange` refuses while a change is unfinished; `RollBackModChange` restores the newest backup that holds the record from before its change, and saves that record; `GetModStatus` reports the record, an unfinished change and the backups. |
| `LargeAddressAwarePatch` / `FixEntry` / `FixPlanner` / `FixPlanText` | Managed fixes (P2-08, [ADR-0013](adr/0013-managed-fixes-in-the-install-folder.md)): the 4 GB patch over an executable's bytes (the flag and the PE checksum, `PortableExecutableChecksum`, nothing else); a fix as held in `library/fixes/<id>/`, with copies of the files it replaces; and the planner that turns applying or removing one fix into a `ModActivationPlan` over the install folder, so the P2-04/P2-05 backup, journal and executor apply it unchanged. `RecordedFile.BeforeSha256` says what a fix replaced. |
| `IManagedFix` / `PlanFix` / `ApplyFix` / `GetFixStatus` | The fix use cases (P2-08): one `IManagedFix` strategy per fix (`LargeAddressAwareFix`; `DxvkFix`, which reads the release through `IFixSourceReader` (`FixSourceReader`: `.tar.gz`, `.zip`, folder or DLL, in memory) and recognizes the DLL with `DxvkDll` and the catalog's fingerprint) prepares the entry to apply from the install folder, read through `IInstallFiles` (`InstallFolderFiles`, which also checks write access and files in use); `PlanFix` plans it without writing anything, `ApplyFix` stores a new entry in `IFixLibrary` (`FolderFixLibrary`) and runs the plan through `ApplyModPlan`, and `GetFixStatus` checks every recorded file against the install folder. `RollBackModChange` and `RecoverModPlan` serve fixes unchanged. |
| `HandInstalledSnapshot` / `AdoptionScanner` / `AdoptionCandidate` / `AdoptionExclusion` | The adoption scan (P2.5-02, [ADR-0015](adr/0015-adopting-hand-installed-mods.md)): what `IHandInstalledModsReader` (`HandInstalledModsReader`) reads from a documents folder (the add-ins `AddIns.xml` registers, the files of each `AddIns` folder and top-level `override` folder, the loose `override` files, and whether Vortex left a `vortex.deployment*.json` there), sorted by the pure `AdoptionScanner` into candidates with stable ids (`addin:<uid>`, `override:<folder>`, `override:(loose)`) and exclusions with a reason: official DLC by the catalog's `Official` entries, an add-in with no folder in the documents folder, a folder with no `AddInItem`. What the deployment record holds is managed and left out. `AdoptionLibraryMatcher` then compares each candidate with the library mods it could be (an add-in with those of its UID, where DAUpdater.exe puts each file; override files with the override mods sharing a file name), hashing only files of the right size through `IDocumentsScanner`, and gives it a `LibraryMatch`: exact, or the closest mod and how many files differ. `ScanForAdoption` fails when the catalog does not load rather than risk offering DLC. `AdoptionPlanner` (P2.5-03) plans an adoption with `ModActivationPlanner`'s own rules: the adopted mods (`AdoptedMod`: a library mod and where each of its files is now) are appended, the planner is shown their files and add-ins as already DAORework's, and its copies to their targets become moves, files already at their targets are only recorded (`AdoptedInPlaceCount`), and an adopted file that loses by name is deleted where it is; program files stay where they are; the plan keeps the real record. `PlanAdoption` builds the `AdoptedMod`s: a candidate that is exactly a library mod (`LibraryMatch.Sources`) is that mod, any other is imported from disk through `IAdoptionImporter` (`HandInstalledModImporter`, on the same `LibraryImporter` as `mods import`, with `AdoptionLayout`'s paths, an `Adopted` origin, and for an add-in a `Manifest.xml` holding its `AddInItem` from `AddIns.xml` as it is); the plan is applied by `ApplyModPlan` like any change. |

## 7. Application layer (use cases)

Each use case:
- is a `sealed class` with a single `public Task<Result<TOut>> ExecuteAsync(TIn request, CancellationToken ct)`;
- takes **at most 5** constructor dependencies. More than that means it should be split;
- contains orchestration only. Business rules belong in Core, and I/O belongs behind a port;
- is registered by `Application/DependencyInjection.cs` (`services.AddApplication()`).

Initial catalog (matching today's features):

| Use case | Replaces |
|---|---|
| `DetectInstallations`, `CreateProfile`, `GetProfile`, `ListProfiles`, `UpdateProfilePath`, `UpdateLaunchTarget` | Logic currently inside `Program.cs` |
| `RunDiagnostics` (+ one `IDiagnosticRule` per check) | `DaoDiagnosticsService` |
| `LaunchGame` | `DaoLauncherService` (no longer auto-saves the profile as a side effect; audit C8) |
| `InspectModFolders`, `BuildModInventory` | `DaoModInspectionService`, `DaoModInventoryService` |

## 8. Cross-cutting concerns

### 8.1 Errors
- **Expected outcomes** (profile not found, executable missing, invalid name) → `Result.Failure(Error)`. An `Error` has a stable `Code`, a `Message`, and an optional `Recommendation`.
- **Unexpected outcomes** (bugs, disk failures in the middle of an operation) → exceptions. They are caught **only** at the presentation boundary, logged with full detail, and turned into a non-zero exit code.
- **Never use `catch { }` or `catch (Exception) { return default; }`.** A file that cannot be read becomes a `DiagnosticIssue` or an inventory entry with `ReadError` set. It is never silently counted as zero.

### 8.2 Logging
- `ILogger<T>` everywhere, with Serilog configured **only** in the composition root.
- Use message templates, not interpolation: `LogInformation("Found {Count} installs", n)`.
- Log levels: `Debug` = internals, `Information` = user-visible milestones, `Warning` = degraded but continuing, `Error` = the operation failed.
- Never log full user paths above `Debug` level in shareable reports without an opt-in, because paths contain Windows usernames.

### 8.3 Diagnostic codes
- Format: `DAO_<AREA>_<CONDITION>`, e.g. `DAO_GAME_EXECUTABLE_NOT_FOUND`.
- Defined once, in `Core/Diagnostics/DiagnosticCodes.cs`. Tests and rules reference the constant, never a string literal (this prevents audit C1 from recurring).
- **A released code is never renamed or reused.** Codes are deprecated, not deleted.

### 8.4 Persistence
- JSON through a single `JsonDefaults.Options`: camelCase, indented, enums written as strings.
- Every persisted document carries `"schemaVersion": n`. Loaders run migrators `n → n+1` in sequence.
- Writes are **atomic**: write to `*.tmp`, then `File.Replace`/`Move`. A crash never leaves half a profile on disk.
- Report file names use `yyyyMMdd-HHmmss-fff` plus a short random suffix (audit C6).

### 8.5 Configuration and paths
- The app-data root comes from `DaoReworkOptions` (default `%LOCALAPPDATA%\DAORework`) and can be overridden for tests.
- `preferences.json` in the app-data root holds the app's own preferences, apart from the launch profiles: the app-wide look and a look per profile ([ADR-0016](adr/0016-gui-themes.md), [ADR-0017](adr/0017-custom-looks.md)).
- Phase 2 adds three folders under it ([ADR-0012](adr/0012-mod-storage-model.md)): `library\<modId>\`, holding each imported mod unchanged with its `mod.json`; `deployments\`, with one record per documents folder of the files DAORework placed there (and, while a change is being applied, its journal); and `deployment-backups\<documentsKey>\<id>\`, the backup taken before each change (P2-04). DAORework writes into the documents folder only through a confirmed plan, and only under `packages\core\override\DAORework\`, `AddIns\<uid>\` and the `AddInItem` entries it added.
- Managed fixes (P2-08, [ADR-0013](adr/0013-managed-fixes-in-the-install-folder.md)) are the only writes to the install folder: the 4 GB flag patched into `bin_ship\daorigins.exe` and a user-supplied DXVK `bin_ship\d3d9.dll`, each through the same plan, backup and journal. Their record is `deployments\<installKey>.json` and their backups `deployment-backups\<installKey>\<id>\`, in the same stores as a documents folder's; the files a fix replaces are kept in `library\fixes\<entryId>\` so it can be removed. DAORework never asks for administrator rights; an install it cannot write to blocks the plan.
- All file-system access goes through `System.IO.Abstractions` (`IFileSystem`), so tests use `MockFileSystem`.
- All time comes from `TimeProvider`, and tests use `FakeTimeProvider`.

### 8.6 Processes
- Launch goes through an `IProcessLauncher` port. Arguments go into `ProcessStartInfo.ArgumentList`, one entry per argument, so quoting is handled correctly (audit C5).

## 9. Presentation

### 9.1 CLI (now)
- Spectre.Console.Cli ([ADR-0005](adr/0005-cli-framework.md)). Each verb is a `Command<TSettings>` class under `Commands/<Feature>/`.
- A command does exactly three things: map the settings to a request, call **one** use case, and hand the result to a renderer.
- Exit codes: `0` success, `1` expected failure (a `Result` failure), `2` invalid usage, `70` unexpected error.
- Renderers live in `Rendering/` and are the only place that writes to the console.

### 9.2 GUI (Phase 1, [ADR-0010](adr/0010-gui-avalonia-in-phase-1.md))
- A separate `DAORework.App` project on **Avalonia**, so a later Linux port stays possible. It starts as the first Phase 1 task and is the primary way to use the tool; the CLI remains for scripting, support bundles and CI.
- View models hold presentation state only. Each page maps user input to one use case and renders its `Result`, exactly as a CLI command does.
- `DAORework.App.Tests` runs on the Avalonia headless platform: screenshot tests for each page and interaction tests for the main flows.
- It consumes **the same Application use cases**. If the GUI needs logic that the CLI does not have, that logic goes into a use case, never into the view model.
- Colors, fonts and motifs come from theme tokens only ([ADR-0016](adr/0016-gui-themes.md)). Views use `{DynamicResource ...}` and never name a color. *Witch of the Wilds* is the default theme, and the user can choose another on the Settings page. A user's own look (a background picture and an accent color over a theme) can be set app-wide or per launch profile ([ADR-0017](adr/0017-custom-looks.md)).

## 10. Testing strategy

| Project | Tests | Rules |
|---|---|---|
| `Core.Tests` | Value objects, domain rules | Pure. No mocks needed. |
| `Application.Tests` | Every use case and every diagnostic rule | `MockFileSystem`, `FakeTimeProvider`, hand-written fakes for the ports. **No real disk.** |
| `Infrastructure.Tests` | Adapters against a real temp directory; JSON round-trips; migrations | Each test gets its own temp dir, deleted afterwards. |
| `Architecture.Tests` | The dependency rules in §4.2, plus naming rules (`sealed`, no `System.IO.File` in Core/Application) | Uses NetArchTest (or reflection), and runs on every build. |
| `Cli.Tests` | Command parsing, exit codes, rendering snapshots | Spectre `CommandAppTester`. |
| `App.Tests` | Page screenshots, main interaction flows | Avalonia headless platform. Screenshots of changed pages are attached to the PR. |
| `FaultInjection.Tests` | The Phase 2 exit gate ([ADR-0014](adr/0014-fault-injection-exit-gate.md)): every mod and fix change, with the real CLI killed before each of its writes; and Phase 2.5's adoptions ([ADR-0015](adr/0015-adopting-hand-installed-mods.md)): an add-in where it is, an override folder, the loose files, two folders sharing a file name | Real temp folders and real processes. The assembly is also the host it kills; settles each kill with recover both ways and checks the exact before- or after-state. |

**Coverage floor:** 80% line coverage on Core and Application, enforced in CI.
Infrastructure and Cli have no floor, but every bug fix needs a regression test.

**Naming:** `MethodOrScenario_Condition_ExpectedOutcome`.
**Style:** Arrange / Act / Assert, one behavior per test.

## 11. Build, CI and delivery

- **Target framework:** .NET 10 LTS ([ADR-0006](adr/0006-target-net10.md)). .NET 8 support ends in November 2026.
- **Quality gates** in `Directory.Build.props`: `TreatWarningsAsErrors`, `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild`, `Nullable=enable`, and central package management (`Directory.Packages.props`).
- **CI** (GitHub Actions, on every PR): restore → `dotnet format --verify-no-changes` → build → test with coverage → fail below the floor.
- **Branching:** `main` is always green and releasable. Work happens on `feature/<short-name>` branches and merges by PR only, with squash-merge. Commit messages follow Conventional Commits (`feat:`, `fix:`, `refactor:`, `docs:`, `test:`, `chore:`).
- **Versioning:** SemVer, tags `vX.Y.Z`, and a `CHANGELOG.md` in Keep-a-Changelog format.

## 12. Security, privacy and legal

- No network access in Phases 0 to 2.5. If network access is ever added, it needs an ADR and user opt-in; the first
  planned is an opt-in update check (ROADMAP P3-06).
- Support reports must offer path redaction (replace `C:\Users\<name>` with `%USERPROFILE%`).
- Mods are untrusted input ([ADR-0018](adr/0018-mod-safety.md)): DAORework never runs anything from a mod, never deploys a program file from one, scans every mod through the user's antivirus (AMSI) before it enters the library and refuses it on a detection, and recognizes known downloads by their SHA-256. Its own releases carry a SHA-256 and a build provenance attestation, and are code-signed once the owner sets signing up (ADR-0018 §4).
- The project is licensed under PolyForm Noncommercial 1.0.0 ([ADR-0020](adr/0020-license-noncommercial.md), superseding the MIT choice in [ADR-0008](adr/0008-project-license-mit.md)): free to use, change and share for any noncommercial purpose, never sold. Existing mods, fixes and tools are incorporated by the three tiers in [ADR-0009](adr/0009-third-party-mods-and-tools.md): detect and advise, manage, reuse permissively licensed code with attribution.
- Any file-format knowledge (Phase 3) comes from **public community specifications and our own observation of file behavior**. The source is cited in `docs/formats/<FORMAT>.md`, and no decompiled code is used. See [LEGAL_BOUNDARIES.md](LEGAL_BOUNDARIES.md).

## 13. Open decisions

These need an owner decision. Each one is recorded as a *Proposed* ADR until it is decided.

| ID | Question | Recommendation |
|---|---|---|
| **D-1** | **What does "remaster" mean for this project?** (a) An enhanced launcher, mod manager and fix-pack companion. (b) An open engine reimplementation that runs on the user's owned assets, in the style of OpenMW or OpenRA. (c) A remake in a commercial engine. | Commit to (a) → the toolkit now. Treat (b) as the stated long-term research goal. **Avoid (c):** it inevitably requires redistributing or re-creating copyrighted content. Phases 0 to 3 are identical for (a) and (b), so nothing is wasted while this is decided. *2026-09-29:* xoreos, the only open engine with Dragon Age: Origins support, has stayed at "areas load, no gameplay" since before its last release in 2020, which confirms (a) now and makes contributing to xoreos the only realistic form of (b) ([ADR-0009](adr/0009-third-party-mods-and-tools.md)). |
| **D-2** | Project name: `DAORework` vs `InquisitorsChoice` (the repo folder name). | **Decided 2026-09-29: `DAORework`.** It already matches the GitHub repository, the solution and every namespace. See [ADR-0007](adr/0007-project-and-type-naming.md). |
| **D-3** | Drop the redundant `Dao` type prefix (`DaoRuntimeProfile` → `RuntimeProfile`) during the Phase 0 restructure? | **Decided 2026-09-29: yes.** Each type is renamed in the Phase 0 task that restructures it. See [ADR-0007](adr/0007-project-and-type-naming.md). |
| **D-4** | GUI technology for Phase 3 (Avalonia vs WPF vs WinUI 3). | **Decided 2026-09-29: Avalonia, starting in Phase 1** (P1-00). See [ADR-0010](adr/0010-gui-avalonia-in-phase-1.md). |
