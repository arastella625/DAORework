# ADR-0002: Clean Architecture with feature-sliced folders and use-case classes

- **Status:** Accepted (P0-01, 2026-09-29)
- **Date:** 2026-09-28

## Context
The existing four projects already follow ports and adapters, but growth is
creating god classes: `Program.cs` (~500 lines), and services that gain one method
per feature and do their own I/O. See CURRENT_STATE_AUDIT A1–A6.

## Decision
- Keep the Core / Application / Infrastructure / Presentation projects and the dependency rule in SDD §4.2, and enforce it with architecture tests.
- Inside each project, organize folders **by feature** (Installations, Profiles, Diagnostics, Mods, Launching).
- Each Application operation is **one use-case class** with a single `ExecuteAsync`. Extensible checks and sources are Strategy implementations (`IDiagnosticRule`, `IInstallLocator`, `IGameRuntime`).

## Consequences
There are more, smaller files, and each one is easy to find, test and review. A new
feature adds files instead of editing hubs. Navigating relies on consistent naming,
so the Standards' naming rules matter.

## Alternatives considered
- *Vertical slice in a single project:* weaker compile-time enforcement of the "no I/O in logic" rule.
- *MediatR pipeline:* commercial license since 2025, and needless indirection at this scale.
- *Keep the service-per-area classes:* this is what produced the current growth problem.
