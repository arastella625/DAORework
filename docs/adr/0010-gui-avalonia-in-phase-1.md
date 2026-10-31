# ADR-0010: Avalonia GUI, starting in Phase 1

- **Status:** Accepted
- **Date:** 2026-09-29

## Context
The roadmap deferred the GUI to Phase 3 and its technology to open decision D-4.
Two things changed. First, the owner wants a window rather than a terminal as
the primary way to use the tool, and the terminal's typed-path commands are
already a usability problem (an install on a second drive needs four
`set-path` commands until P1-01). Second, everything a GUI needs is what
Phase 0 is finishing: the use cases it must call already exist, and the SDD
rule that the GUI consumes the same use cases as the CLI makes the shell thin.

A GUI also makes verification visual: Avalonia's headless test host renders
screens to images, so pull requests can carry real screenshots produced in CI.

## Decision
- **D-4 is decided: Avalonia** (MIT, cross-platform, keeps a Linux/Proton port
  possible). WPF and WinUI 3 are Windows-only and would close that door.
- The GUI shell (`DAORework.App`) is the **first Phase 1 task, P1-00**, not a
  Phase 3 task. It ships a profile page with folder pickers, a diagnostics page,
  a mods page and a launch action, all over existing use cases.
- Phase 1 features land in the GUI as they arrive. The CLI stays, as the tool
  for scripting, support bundles and CI checks, and P0-12 remains a pure
  restructure: no interactive terminal mode, since the window replaces it.
- `DAORework.App` follows the same dependency rule as `DAORework.Cli`: it
  references Application, Core, and Infrastructure only in its composition root.
  Logic the GUI needs that the CLI lacks goes into a use case, never a view model.
- `DAORework.App.Tests` uses the Avalonia headless platform for screenshot and
  interaction tests. Screenshots of changed screens are attached to the PR.

## Consequences
Phase 1 grows by the shell task, and each Phase 1 feature is surfaced twice
(CLI and GUI) until the CLI becomes secondary. Packaging (P1-06) must publish
the App rather than the CLI. Phase 3's GUI items shrink to the toolkit views.

## Alternatives considered
- *Keep the GUI in Phase 3:* the tool would stay terminal-only for the whole
  companion phase, against the owner's stated preference.
- *Interactive terminal mode via Spectre.Console prompts:* would be thrown away
  when the window arrives, and is a new feature inside the no-new-features phase.
- *WPF or WinUI 3:* mature, but Windows-only.
