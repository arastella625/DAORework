# ADR-0005: Spectre.Console.Cli for the command-line interface

- **Status:** Accepted (P0-01, 2026-09-29)
- **Date:** 2026-09-28

## Context
Arguments are currently parsed by hand with nested `switch` statements and
positional `args.Skip(n)` in `Program.cs`. This produces the god file, inconsistent
help text and a dead `profiles` command (audit A1, C2).

## Decision
Use **Spectre.Console.Cli**. Each verb is a `Command<TSettings>` or
`AsyncCommand<TSettings>` class with a typed settings class. Commands are
constructed through DI via an `ITypeRegistrar` adapter. Rendering uses
Spectre.Console tables and markup, and is isolated in `Rendering/`.

## Consequences
Help, validation and branch commands (`profile show`, `mods inventory`) are
generated from the types. `Program.cs` becomes a composition root of about 40
lines. Tests use `CommandAppTester`.

## Alternatives considered
- *System.CommandLine:* first-party and now stable, but more code is needed to get one-class-per-command and it has no rendering story. It is a reasonable alternative if we want only first-party dependencies.
- *Keep hand-rolled parsing:* rejected, because this is the cause of the current issues.
