## Summary

<!-- What changes and why. Link the issue: "Closes #NN". One task, one PR. -->

Closes #

## Checklist (ENGINEERING_STANDARDS.md §7)

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

## New packages

<!-- One line of justification per new package, or "None". A framework-level package needs an ADR. -->

None
