# ADR-0006: Target .NET 10 LTS

- **Status:** Accepted (P0-01, 2026-09-29)
- **Date:** 2026-09-28

## Context
The projects target `net8.0`, whose support ends on 2026-11-10. The development
machine has SDK 10.0.401. .NET 10 is the current LTS, supported until November 2028.

## Decision
Target `net10.0` for all projects, set in `Directory.Build.props`. Pin the SDK with
`global.json` (`rollForward: latestFeature`).

## Consequences
We get current C# language features, keyed DI and `TimeProvider` improvements.
Users of the published single-file build do not need a separate runtime install.

## Alternatives considered
- *Stay on .NET 8:* it would be out of support before Phase 1 ships.
