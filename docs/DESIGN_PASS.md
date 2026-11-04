# Design pass: a proposal

**Status:** for the owner to read. Nothing here is built. Any layout change below needs an ADR that amends
[ADR-0026](adr/0026-workflow-first-layout.md) first.

**The aim:** the window should never be the hardest part of modding. The test is `RealScaleLibrary`: 70 mods, 8,000 files.

The notes on other tools come from general knowledge of how they work, not from a fresh review of current versions.
They describe patterns only: no product's look or assets are copied.

## What comparable tools do well

| Pattern | Seen in | Why it helps |
|---|---|---|
| Drag a mod to reorder it, with several selected at once | Mod Organizer 2, most load-order tools | Moving a mod 50 places is one gesture. |
| A small mark on each row: this mod wins, loses or both | Mod Organizer 2 | Conflicts are visible in the list, before anything is opened. |
| Search box over the list | Nearly all of them | With 70 mods, finding one by scrolling is slow. |
| Sort and group: by category, date added, state | Vortex, Mod Organizer 2 | The list has a shape instead of being one long column. |
| Rows that stay one line, with details in a side panel | Vortex, Steam's library | Many rows fit, and detail is one click away. |
| Saved sets of mods, switched in one step | Mod Organizer 2, r2modman | Trying a setup costs nothing. |
| One place for "needs attention" | Vortex | Problems wait in one list instead of on many pages. |
| A first-run walk through setup | Newer launchers | A new player has a next step. |

## What to take, in order

1. **One-line rows and a details panel.** Today a row is four lines, so 70 mods is a long scroll, and antivirus text
   repeats on every row. Rows become name, kind, a conflict mark and the switch. Selecting one opens its details beside
   the list. *Layout change: needs an ADR.*
2. **Search, and sort by name, newest or state.** No ADR needed: it fits the existing filter row.
3. **Conflict marks on rows.** *Wins*, *loses*, *both*, in amber, each opening that mod's shared files. Never on `.erf`
   contents, where the winner isn't known.
4. **Drag to arrange**, with *Move to top* and *Move to bottom*, and one preview for the whole move. Arrange's Up and
   Down cost one preview per step.
5. **A real preview panel.** It opens beside or over the list, so it doesn't depend on scrolling, and its buttons always
   show. *Layout change: needs an ADR.*
6. **A quieter preview.** Problems stay amber; grouped notes become plain text, since a screen of amber stops meaning
   anything.
7. **A cozy guide**, as the owner has asked: a short first-run path to *Add mods*, *Turn on*, *Play*.

## What to keep

The six tabs, the plain words, the gilded-tome style, one main button per screen, and previews before every change.
These are what sets DAORework apart, and nothing above needs them changed.

## What to leave out

- A built-in browser or Nexus sign-in: ADR-0034 and P3-12 cover that.
- Plugin systems, rules languages and a virtual file system: they serve experts, and cost the plainness.

## Questions for the owner

1. Is a details panel beside the list right, or should a row expand in place?
2. Should the preview open beside the list, or as a sheet over it?
3. Is drag enough for Arrange, or are numbered positions wanted too?
4. Are saved sets of mods wanted now, given profiles exist already?
