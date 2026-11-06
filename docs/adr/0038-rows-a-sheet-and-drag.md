# ADR-0038: One-line rows, a preview sheet and drag to arrange

- **Status:** Proposed
- **Date:** 2026-11-05

## Context
[DESIGN_PASS.md](../DESIGN_PASS.md) proposed layout changes that need an ADR amending
[ADR-0026](0026-workflow-first-layout.md). With 70 mods, a four-line row makes a long scroll, and the same antivirus line
repeats on every row. The owner answered its questions on 2026-11-05.

## Decision
- **A mod's row is one line:** its name, what kind of mod it is, anything worth a glance, and the switch. Clicking the name
  expands the row in place. It keeps the player's place in the list and works at the window's smallest width.
- **What stays on the row:** a warning. Anything that needs the player's attention is never behind a click.
- **What moves behind it:** file counts, where a mod came from, and the antivirus verdict. ADR-0018's wording is unchanged,
  and a mod that wasn't scanned says so when expanded. *(This narrows where ADR-0018's notes show, not what they say.)*
- **The preview opens as a sheet over the list**, with its buttons always visible.
- ***Arrange* gets drag,** with one preview for the whole move, and no numbered positions.

The six tabs, the plain words, the gilded-tome style and previews before every change stay as ADR-0026 has them.

## Consequences
- Far more mods fit on a screen.
- A player has to expand a row to see that it wasn't scanned. A failed scan never reaches the library, so nothing dangerous
  is hidden, but this is a real trade.
- *Found in your game* rows change separately, once the lists merge.

## Alternatives considered
- **A details panel beside the list.** Shows more at once, but squeezes the list at the minimum width.
- **A preview panel beside the list.** The same squeeze.
- **Numbered positions in *Arrange*.** Adds a box to every row for a rare need.
