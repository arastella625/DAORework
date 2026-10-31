# ADR-0007: Project name and type naming

- **Status:** Accepted
- **Date:** 2026-09-29

## Context
The project has carried two names: `InquisitorsChoice` (the original local folder)
and `DAORework` (the GitHub repository, the solution file and every namespace).
Every type also carries a `Dao` prefix (`DaoRuntimeProfile`, `DaoInstallation`)
inside a `DAORework.*` namespace, so the prefix repeats what the namespace says.
Phase 0 restructures nearly every file, so this is the cheapest moment to settle
both (SDD open decisions D-2 and D-3, audit item D8).

## Decision
- The project name is **`DAORework`**, everywhere: repository, solution, project
  files, namespaces, app-data folder and user-facing text.
- Types drop the `Dao` prefix: `DaoRuntimeProfile` becomes `RuntimeProfile`,
  `DaoInstallation` becomes `Installation`, and so on. The rename happens inside
  the Phase 0 task that already restructures each type (P0-06 to P0-12), never as
  a standalone rename PR.
- Diagnostic codes keep their `DAO_` prefix. They are a released, stable vocabulary
  (SDD §8.3), not type names.

## Consequences
No renaming work is needed for D-2. Reviewers of Phase 0 PRs should expect the
type renames and check that each one lands with its restructure, not before.

## Alternatives considered
- *`InquisitorsChoice`:* would require renaming the solution, four projects and every namespace.
- *Keep the `Dao` prefix:* less churn now, but the redundancy would be permanent and every new type would copy it.
