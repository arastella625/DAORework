# CLAUDE.md

Guidance for AI assistants working in this repository.

## Read before changing code
1. `docs/ROADMAP.md`: find the current phase and the task ID you are working on. Do not work ahead of the current phase.
2. `docs/SDD.md`: layering (§4), prescribed patterns (§5) and cross-cutting rules (§8).
3. `docs/ENGINEERING_STANDARDS.md`: hard limits and the PR checklist. These are mandatory.
4. `docs/LEGAL_BOUNDARIES.md`: never add game assets, decompiled code or DRM workarounds.

## Non-negotiables
- No file over 300 lines, no method over 50 lines, and no class with more than 5 constructor dependencies.
- No `File`/`Directory`/`Registry`/`Process`/`Environment`/`DateTime.Now` usage outside `DAORework.Infrastructure`.
- New behavior goes in a **new** use case, rule or strategy class, never as an extra method on an existing service.
- Expected failures return `Result<T>`. Never swallow exceptions or return sentinel values.
- Diagnostic codes come from `DiagnosticCodes` constants, and a released code is never renamed.
- A design choice not covered by the SDD needs an ADR in `docs/adr/` (`0000-template.md`) first.
- Refactor PRs change no behavior, and feature PRs contain no unrelated refactors.

## Commands
```bash
dotnet build DAORework.sln
dotnet test DAORework.sln
dotnet run --project src/DAORework.Cli -- --help
```
