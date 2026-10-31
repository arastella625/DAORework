# Current State Audit

**Date:** 2026-09-28
**Baseline commit:** `78f10d1`
**Build:** succeeds, 2 warnings
**Tests:** 1 total, **1 failing**

This is a snapshot of the codebase measured against [SDD.md](SDD.md) and
[ENGINEERING_STANDARDS.md](ENGINEERING_STANDARDS.md). Every finding maps to a
Phase 0 task in [ROADMAP.md](ROADMAP.md). Do not start Phase 1 feature work
until every finding marked **Blocker** or **High** is closed.

---

## What is already good

- **The four layers are real projects** (`Core`, `Application`, `Infrastructure`, `Cli`),
  and the project references point the right way: Core has no dependencies, and
  Infrastructure depends on Application.
- **Ports exist.** Application defines interfaces (`IDaoInstallFinder`,
  `IDaoRuntimeProfileStore`, ...) and Infrastructure implements them.
- **The runtime already uses Strategy + Factory** (`IDaoRuntime`, `DaoRuntimeFactory`), and
  the experimental runtime deliberately fails. That is the right instinct.
- **Diagnostics are data**, not console text: `DiagnosticIssue` has a severity, a code, a
  message and a recommendation. That makes reports machine-readable.
- **The legal boundaries are written down** (`docs/LEGAL_BOUNDARIES.md`).
- **Nullable reference types** are on, with `CancellationToken` in every async signature.

The foundation is sound. The problems below come from growth, not from the design.
They are cheap to fix now and expensive later.

---

## Findings

Severity: **Blocker** = the codebase is not trustworthy until fixed; **High** = will
compound with every feature; **Medium** = fix during Phase 0; **Low** = cleanup.

### A. Architecture violations

| # | Severity | Finding | Where |
|---|---|---|---|
| A1 | **High** | **The CLI is a god file.** `Program.cs` is ~500 lines holding composition, arg parsing, 12 command handlers, business logic (profile creation, default-path rules) and all rendering. Every new command makes it bigger. | `src/DAORework.Cli/Program.cs`. **Closed by P0-12:** Spectre.Console.Cli, one command class per verb, renderers, `Program.cs` is 29 lines. |
| A2 | **High** | **The Application layer does direct file-system I/O** (`File.Exists`, `Directory.EnumerateFiles`, `FileInfo`, `SHA256` over files). Use cases cannot be unit-tested without a real disk, and Application is no longer pure orchestration. | `DaoDiagnosticsService`, `DaoModInspectionService`, `DaoModInventoryService`. **Closed by P0-07:** `IFileSystem`, `IFileHasher`, `IKnownFolders`, `TimeProvider`; the architecture rule is now enforced. **P0-08** replaced `DaoDiagnosticsService` with the `RunDiagnostics` use case and five `IDiagnosticRule` classes. |
| A3 | **High** | **The Core domain does I/O.** `DaoInstallation.HasValidGameExecutable` calls `File.Exists` inside a domain model, so its result depends on the machine state when it is read. | `Core/Installations/DaoInstallation.cs`. **Closed by P0-07:** validity is data captured at detection time. |
| A4 | Medium | **Business logic lives in the CLI.** `InitAsync` builds a profile and chooses the default documents path. That is a use case, not presentation. | `Program.cs` `InitAsync`. **Closed by P0-11:** `CreateProfile`, `GetProfile`, `ListProfiles`, `UpdateLaunchTarget`, `UpdateProfilePath` and `DetectInstallations` use cases. |
| A5 | Medium | **Infrastructure owns an Application service registration** (`DaoLauncherService`, `DaoDiagnosticsService`, ...). Each layer should register its own services. | `ServiceCollectionExtensions.cs`. **Closed by P0-11:** `AddApplication()` and `AddInfrastructure(options)`, paths bound through `DaoReworkOptions`. |
| A6 | Medium | **Services are resolved from a container** in the CLI (`services.GetRequiredService<...>` inside every handler), which is a service-locator pattern. | `Program.cs`. **Closed by P0-12:** commands receive their use cases and renderers by constructor; the container is touched only by the Spectre resolver. |

### B. Duplication (the same knowledge in several places)

| # | Severity | Finding | Where |
|---|---|---|---|
| B1 | **High** | **DAO path conventions are hard-coded in five places.** `Documents\BioWare\Dragon Age`, `packages\core\override`, `AddIns`, `bin_ship\daorigins.exe` and `DAOriginsLauncher.exe` each appear in 2 to 4 files. One change to the game's layout knowledge means editing all of them. | Diagnostics, Inspection, Inventory, Launcher, Program, InstallFinder. **Closed by P0-06:** `GamePaths` in Core. |
| B2 | Medium | `FormatBytes` is copy-pasted into two models. | `DaoModFolderInspection`, `DaoModInventoryReport`. **Closed by P0-06:** `ByteSize`. |
| B3 | Medium | The JSON options are defined per class, and they are **inconsistent**: 3 writers use `JsonSerializerDefaults.Web` (camelCase), and the inventory writer uses default settings (PascalCase). As a result, report files have mixed naming. | `Infrastructure/*`. **Closed by P0-06:** `JsonDefaults` (camelCase, enums as strings). |
| B4 | Low | The "profile not found → print hint" block is copy-pasted into five CLI handlers. | `Program.cs`. **Closed by P0-12:** one `ErrorRenderer`. |

### C. Correctness bugs

| # | Severity | Finding | Where |
|---|---|---|---|
| C1 | **Blocker** | **The only test fails.** It expects code `DAO_EXECUTABLE_MISSING`, but the service emits `DAO_GAME_EXECUTABLE_MISSING`. The suite has been red since the rename. | `tests/.../DaoDiagnosticsServiceTests.cs:17`. **Closed by P0-02:** the test references `DiagnosticCodes.GameExecutableMissing`. |
| C2 | **High** | **The `profiles` command is advertised in `help` but is not wired up.** It falls through to `help`. `ListProfilesAsync` is dead code (compiler warning CS8321). | `Program.cs`. **Closed:** wired in P0-03; P0-12 adds `profile list` as an alias. |
| C3 | **High** | **Errors are swallowed silently.** `catch { return 0; }` and `catch { continue; }` hide unreadable files, and `ComputeSha256` returns `""` on failure. A report can claim "no duplicates" when it simply could not read the files. | Inspection, Inventory. **Closed by P0-09:** an unreadable file is listed with `ReadError` set, folder summaries carry `UnreadableFileCount`, and an unlistable folder fails the use case with `DAO_MOD_FOLDER_UNREADABLE`. |
| C4 | Medium | **Unhandled exceptions exit with code 0.** The top-level `catch` logs the error and falls through, so scripts and CI cannot detect a failure. | `Program.cs`. **Closed by P0-12:** exit codes 0 success, 1 expected failure, 2 invalid usage, 70 unexpected. |
| C5 | Medium | **Launch arguments are joined with spaces without quoting,** so an argument containing a space splits into two. | `OriginalGameRuntime.cs`. **Closed by P0-07:** the new `ProcessLauncher` adapter uses `ArgumentList`. |
| C6 | Medium | **Report file names have one-second resolution,** so two reports written in the same second overwrite each other. | Report writers. **Closed by P0-13:** `ReportFileWriter` names reports `yyyyMMdd-HHmmss-fff` plus a random suffix and never overwrites; a taken name gets a new suffix. |
| C7 | Medium | **Profile names are sanitized one way on save and not reversed on list.** `My:Profile` saves as `My_Profile.json`, lists as `My_Profile`, and can then no longer be loaded as `My:Profile`. Nothing validates names up front. | `JsonDaoRuntimeProfileStore`. **Closed by P0-06:** `ProfileName` validates up front and names are stored verbatim; the P0-13 migrator should normalize any legacy profile whose stored name is invalid. **P0-13 added it:** the v0 → v1 migration sets the stored name to the file stem. |
| C8 | Low | `DaoLauncherService.LaunchAsync` **silently saves a profile** as a side effect of launching. A read-looking command writes state. | `DaoLauncherService.cs`. **Closed by P0-10:** the `LaunchGame` use case writes only the diagnostic report; an unknown profile is a `DAO_PROFILE_NOT_FOUND` failure. Runtimes are keyed services behind `IGameRuntimeResolver`. |

### D. Standards and hygiene

| # | Severity | Finding |
|---|---|---|
| D1 | **High** | **No quality gates:** `TreatWarningsAsErrors=false`, no analyzers, no `.editorconfig`, no CI. Warnings already exist and nothing stops more. **Gates added by P0-03** (`TreatWarningsAsErrors`, analyzers, StyleCop, `.editorconfig`); **CI added by P0-04** (format, build, test, coverage ratchet, file-size gate). Closed. |
| D2 | **High** | **Test coverage is effectively zero.** The test project does not reference Infrastructure, and there are no architecture tests to enforce the dependency rules. *P0-04 measured the baseline: Core 17%, Application 16%; the CI ratchet starts at 15%.* **P0-05 split the tests into five projects and added the architecture tests; the two forbidden-API rules are skipped until P0-07.** **P0-07 brought Core and Application above 80% and set the CI floor to 80. Closed.** |
| D3 | **High** | **The target is `net8.0`, whose support ends in November 2026** (about 6 weeks away). The installed SDK is 10.0.401, and .NET 10 is the current LTS. **Closed by P0-03.** |
| D4 | Medium | Domain models are mutable (`{ get; set; }`) and expose `List<T>`/`Dictionary<,>` publicly. **Closed by P0-07:** `init` only, records for profiles, read-only collections. |
| D5 | Medium | Persisted JSON has **no schema version**, so there is no migration path once users have saved profiles. **Closed by P0-13:** every document carries `schemaVersion`, loads run `JsonMigrationChain` (v0 → v1 for profiles), and writes go through `AtomicJsonFile` (`*.tmp` then move). |
| D6 | Low | The file name `DAOLaunchTarget.cs` does not match the type name `DaoLaunchTarget`. There are unused `using` blocks, one file has wrong indentation (`DaoModInventoryItem.cs`), and there is a duplicate `using` (warning CS0105). **Closed by P0-03.** |
| D7 | Low | The README's tree diagram is mojibake (UTF-8 read as CP-1252), and the README lists a `profiles` command that does not work. **Closed by P0-14:** the README is rewritten from the real `--help` output (P0-12 made `profiles` work), and `.gitattributes` pins LF. |
| D8 | Low | **Naming is inconsistent:** the repo folder is `InquisitorsChoice`, the GitHub repo and solution are `DAORework`, and every type carries a redundant `Dao` prefix inside a `DAORework.*` namespace. See the open decisions in the SDD. **Closed by P0-01:** ADR-0007 fixes the name as `DAORework` and drops the prefix during the restructure. |

---

## Phase 0 re-run

**Date:** 2026-09-29
**Commit:** `624c52f` (`main` after P0-13)
**Build:** succeeds, 0 warnings, with `TreatWarningsAsErrors`
**Tests:** 181 total, 0 failing, 0 skipped (Core 59, Application 50, Infrastructure 51, Architecture 9, Cli 12)

Measured with the same commands CI runs (`dotnet build`, `dotnet test --collect:"XPlat Code Coverage"`,
`scripts/check-coverage.ps1`, `scripts/check-file-size.ps1`).

| Measure | Baseline (2026-05-26) | Re-run | Gate |
|---|---|---|---|
| Line coverage, Core | 17% (measured at P0-04) | 98.9% | ≥ 80% (CI) |
| Line coverage, Application | 16% (measured at P0-04) | 94.1% | ≥ 80% (CI) |
| Line coverage, Infrastructure | not measured | 88.6% | none |
| Line coverage, Cli | not measured | 75.6% | none |
| Largest source file | `Program.cs`, ~500 lines | `BuildModInventory.cs`, 130 lines | ≤ 300 (CI) |
| Source files / lines under `src/` | 4 projects | 111 files, 3,132 lines | |
| Architecture tests | none | 9, none skipped | must be green |
| `File`/`Directory`/`Process`/`Environment`/`DateTime.Now` outside Infrastructure | throughout | none (enforced by `ForbiddenApiTests`) | |
| Direct container access in the CLI | every handler | none (Spectre resolver only) | |
| Persisted documents with `schemaVersion` | none | all (profiles, mod profiles, reports) | |

**Findings:** all 26 (A1–A6, B1–B4, C1–C8, D1–D8) are closed; each row above says which task closed it.
**Open High/Blocker items:** none.

The remaining Phase 0 exit-gate items are outside this document: P0-15 (dev container, Dependabot)
and the manual CLI check against a real install, which `scripts/verify-real-install.ps1` has run on
every code task since P0-10 (see CONTRIBUTING.md, *Verify before the PR*).

### Observations carried into Phase 1

These are not findings against the SDD, but they are the loose ends a future reader would want named.

- **Cli coverage is 75.6% and not gated.** The uncovered lines are `Program.cs`, `CliLogging` and the Serilog composition, which the `CommandAppTester` host replaces. The GUI (P1-00) should get the same headless end-to-end treatment from day one.
- **A document newer than the build throws** (`InvalidDataException`, exit 70) instead of returning a `Result`. The `IProfileRepository` port still returns `RuntimeProfile?`; if a Phase 1 feature needs to show that case in the GUI, change the port to `Result<RuntimeProfile>` then.
- **`IDaoModProfileService` is registered but no use case uses it.** It waits for Phase 2 (P2-01, the mod storage ADR), which may replace it.
- **`WellKnownPathLocator` only checks fixed folders.** P1-01 replaces it with the Steam library, GOG and EA App locators.
