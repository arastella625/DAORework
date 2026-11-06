# ADR-0040: What can be combined, and in what order

- **Status:** Proposed
- **Date:** 2026-11-06

## Context
ADR-0025 combines `chargenmorphcfg.xml`. Every other file two mods share still has one winner, and the owner asked for
more: work out where mods can be made compatible, and amalgamate them where possible. P3-08 left combining tables to its
own ADR, "with the M2DA mechanism as the first answer". This is that ADR.

Counted on the owner's library on 2026-11-06, once its copies were cleared (63 entries, 6,406 game file names): 50 names
differ, where 252 did before, so most of those were copies.

| Kind | Names that differ |
| --- | --- |
| `.dds` textures | 44 |
| `.ncs` scripts | 3 |
| `.mao` materials | 2 |
| `.cut` cutscenes | 1 |
| `.gda` tables, `.tlk` talk tables | 0 |

Most of the textures are *JB3* against the armour and clothing mods. `rules_core.ncs`, in seven versions, and
`talent_singletarget.ncs` are mostly options inside one mod, *DA2 Style 2-Handed Weapons*, not two mods clashing. What
clashes inside `.erf` archives isn't in this count. It is one library, so it says what to do first, not what never to do.

Not every kind of file can be combined:

| Kind | Can it be combined? | Why |
| --- | --- | --- |
| `chargenmorphcfg.xml` | Done | ADR-0025 |
| `.gda` data tables | Yes, by row | Rows have IDs. The [toolset wiki](https://datoolset.net/wiki/2DA) describes M2DA tables, which the game itself builds from fragments |
| `.tlk` talk tables | Perhaps, by string ID | P3-09 comes first |
| `.erf` archives | Perhaps | P3-07 already reads what is inside, so a clash can be seen before anything is tried |
| `.ncs` scripts | No | Compiled code. Two versions can't be merged without the source |
| Textures and models | No | Only one look can show. The choice is which |

## Decision
- **The count sets the order.** Nothing on this library needs tables combined, so that waits until a library does:
  1. **Choose who wins for each file, not only each mod.** 46 of the 50 are looks, which can't be combined. A player can
     still want *JB3*'s robes and another mod's boots.
  2. **Options inside a mod.** A mod that ships several versions of a file means the player to pick one, so offer that.
  3. **Count what clashes inside `.erf` archives,** which this count couldn't see.
  4. **Tables,** as below, when a library has them.
- **Reading tables comes first.** The GFF reader (P3-01) and P3-08 come before any combining of tables. The format is
  confirmed on real files, from public specifications and clean-room work only.
- **The base game's table tells a change from a copy.** A mod that ships a whole table looks as if it changed every row.
  Comparing with the player's own game shows what each mod really changed. This reverses ADR-0025's rule for tables
  only, and it reads the game's archives on the player's PC. No game content enters the repository, and tests use
  made-up tables (LEGAL_BOUNDARIES).
- **M2DA first.** The table that wins stays as it is. Rows that the losing mods changed go into a fragment DAORework
  generates, and the game merges it. The [wiki](https://datoolset.net/wiki/Extending_GDAs_tutorial) says a fragment can
  override a row by reusing its ID. That has to be seen working in the real game before this is built on.
- **Other tables are written whole,** only if the count shows they matter.
- **A generated file is owned as ADR-0025's is:** kept in `library/_combined/`, deployed from there, recorded, backed up
  and worked out again by each plan.
- **Two mods changing one row is a real clash.** The load order decides, and the plan names the row and the mods.
- **What can't be combined says so.** A script clash says it needs a patch from the mods' authors and names both mods.
  Known patches can join the catalog (ADR-0009), as data.
- **A combined file stays on the player's PC.** Nothing combined is uploaded or shared, since the mods' permissions
  don't cover that.

## Consequences
- Tables get the same treatment as `chargenmorphcfg.xml`, and undoing works the same way.
- DAORework reads the game's archives for mod planning for the first time, and writes a game format for the first time.
- A combined table can still break a mod that relied on winning a row. The plan has to say what it did.
- `rules_core.ncs` will not be solved this way. That has to be said plainly.

## Alternatives considered
- **Combine without the base game.** Simpler, but it can't tell a whole-table copy from a real change, so one mod's
  changes would be lost with no warning.
- **Always write whole tables.** Works for every table, but replaces more of the game than a fragment does.
