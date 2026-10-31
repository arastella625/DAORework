# Engineering Standards

These rules are **mandatory** for every change. A human or AI reviewer rejects a PR
that breaks one without a written, reviewed exception (an ADR, or a
`// STANDARDS-EXCEPTION: <reason>` comment on the line). Wherever possible, a rule
is enforced by tooling rather than by memory. The **Enforced by** column says how.

## 1. Size and responsibility limits (no god files)

| Rule | Limit | Enforced by |
|---|---|---|
| Lines per file | soft 200, **hard 300** | CI script (`scripts/check-file-size.ps1`; temporary exemptions in `scripts/file-size-exceptions.txt`, each naming the task that removes it) |
| Types per file | **1** (nested private types excepted) | StyleCop.Analyzers SA1402 |
| Lines per method | soft 30, **hard 50** | code review |
| Constructor dependencies | **≤ 5**; more means split the class | architecture test |
| Public methods on a use case | **1** (`ExecuteAsync`) | architecture test |
| Cyclomatic complexity per method | ≤ 10 | CA1502 (enabled via `.editorconfig` + `CodeMetricsConfig.txt`) |
| Parameters per method | ≤ 5; more means introduce a request record | code review |

**If you are about to add a method to an existing service, stop:** it is probably a
new use case, rule or strategy, and belongs in a new file.

## 2. Layering

- Follow the dependency table in [SDD §4.2](SDD.md#42-the-dependency-rule-enforced-by-architecture-tests-see-10). Architecture tests fail the build if you break it.
- **No static I/O outside Infrastructure.** Do not use `File.*`, `Directory.*`, `new FileInfo`, `Registry.*`, `Process.Start`, `Environment.GetFolderPath`, `DateTime.Now` or `DateTimeOffset.UtcNow` in Core or Application. Use `IFileSystem`, a port, or `TimeProvider`.
- **No business logic in Presentation.** A CLI command maps input → one use case → renderer.
- **No service locator.** Never call `IServiceProvider.GetService` outside the composition root and the keyed-service resolvers.
- Each layer registers its own services: `AddCore()`/`AddApplication()`/`AddInfrastructure()`.

## 3. C# style

- **File-scoped namespaces.** The namespace matches the folder path.
- **`sealed` by default.** Unseal only with a reason.
- **Records for data.** Use `sealed record` for DTOs, requests, results and value objects.
- **Immutability.** Use `init` over `set`. Public collections are `IReadOnlyList<T>`/`IReadOnlyDictionary<,>`.
- **Primary constructors** are allowed for DI-only classes. Assign them to `private readonly` fields when the value is used in more than one method.
- **Nullability.** No `!` null-forgiving operator without a comment explaining why the value cannot be null.
- **Async.** Every I/O method is `async` and takes `CancellationToken ct` as its last parameter. No `.Result`/`.Wait()`. Suffix async methods with `Async`, except use-case `ExecuteAsync`, which already has it.
- **No magic strings.** Diagnostic codes, file names and folder names come from `DiagnosticCodes` and `GamePaths`.
- **Naming:** `PascalCase` types and members, `_camelCase` private fields, `camelCase` locals and parameters, `I` prefix for interfaces. File name = type name, exactly (case included).
- **Formatting** is defined by `.editorconfig` and checked by `dotnet format --verify-no-changes` in CI.

## 4. Errors and logging

- Expected failure → `Result<T>`. A bug or unexpected I/O failure → exception.
- **Forbidden:** empty `catch`, `catch (Exception)` outside the top-level boundary, and returning sentinel values (`""`, `0`, `null`) to hide a failure.
- Catch the **specific** exception (`IOException`, `UnauthorizedAccessException`), turn it into a domain outcome, and log it at `Warning`.
- Log with structured templates, never string interpolation.

## 5. Testing

- **Every PR that changes behavior adds or changes a test.** Every bug fix starts with a failing test.
- Core and Application tests never touch the real disk, registry, clock or processes.
- Test names: `Scenario_Condition_ExpectedOutcome`. Structure: Arrange / Act / Assert. One behavior per test.
- Reference constants (such as `DiagnosticCodes.GameExecutableMissing`), never copy the literal.
- Coverage floor: 80% line coverage on Core and Application (CI gate).
  Until the Phase 0 exit gate, CI enforces a **ratchet** (`COVERAGE_FLOOR` in `.github/workflows/ci.yml`, checked by `scripts/check-coverage.ps1`): it starts at the measured baseline and is only ever raised. It reached 80 in P0-07.
- A red `main` is a stop-the-line event: fix or revert before any other merge.

## 6. Dependencies

- Versions live only in `Directory.Packages.props` (central package management).
- A new package needs a one-line justification in the PR description. A new *framework-level* package (DI, CLI, UI, ORM, messaging) needs an ADR.
- Prefer the BCL and `Microsoft.Extensions.*`. Avoid packages with commercial or unclear licenses.

## 7. Git and PRs

- Branches: `feature/…`, `fix/…`, `refactor/…`, `docs/…`. Never commit directly to `main`.
- Commit messages follow Conventional Commits: `feat(mods): add conflict scanner`.
- PRs stay small: about **≤ 400 changed lines** of production code. Split larger work.
- **A refactor PR changes no behavior, and a feature PR does no unrelated refactoring.**

### PR checklist (copied into the PR template)

- [ ] Follows the SDD layering and patterns. Any new pattern or framework has an ADR.
- [ ] No file > 300 lines, no method > 50 lines, no class with > 5 dependencies.
- [ ] No static I/O, clock or environment access outside Infrastructure.
- [ ] No swallowed exceptions or sentinel error values.
- [ ] Tests added or updated; `dotnet test` is green locally.
- [ ] `dotnet format --verify-no-changes` is clean, with zero warnings.
- [ ] New diagnostic codes are added to `DiagnosticCodes` and documented. No code was renamed.
- [ ] Persisted schema changes bump `schemaVersion` and include a migrator and a test.
- [ ] README/docs are updated if user-facing behavior changed.
- [ ] Nothing here conflicts with LEGAL_BOUNDARIES.md.

## 8. Definition of Done

A task is done when **all** of the following are true:
1. The code is merged to `main` via PR, and CI is green.
2. The PR checklist is satisfied.
3. Tests cover the new behavior, and coverage is at or above the floor.
4. User-facing changes appear in `CHANGELOG.md` under *Unreleased*.
5. The docs (SDD, README, the audit if applicable) reflect reality.
