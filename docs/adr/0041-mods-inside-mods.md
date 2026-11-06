# ADR-0041: Mods inside other mods

- **Status:** Accepted
- **Date:** 2026-11-06

## Context
A library can hold *ZDF* and *ZDF Complete*, where every file of the first is inside the second. [ADR-0039](0039-look-first-and-keep-one-copy.md)
counts that as a part only when the two share a name or the bigger one is on, and it rejected treating any mod inside
another as a part: a small mod with its own name may be the one the player wants. So *ZDF* is never mentioned, and a
player can turn both on and wonder why they overlap.

A mod's `.erf` also hides what it holds. Two mods can package the same resources in different archives, and the hashes of
the archives say nothing about that.

## Decision
1. **Say so and offer, never preset.** A mod whose files are all in a bigger mod gets a note on its row, *Everything in this
   mod is also in ZDF Complete*, and a *Keep ZDF Complete* button. It does not join a set of copies, so *Keep one of each*
   and the next step stay as ADR-0039 has them, and nothing is picked for the player.
2. **The button is *Keep this one* for those two mods.** It shows the same preview and follows the same rules: removing is
   permanent and says so, a mod that is on gives its place, and a mod on in another profile stops it.
3. **Look inside an `.erf` by name.** A file counts as inside the bigger mod if its name and hash are there. An `.erf` also
   counts if every resource in it is in the bigger mod, inside an archive or loose. Only the table of contents is read,
   which holds no hash, so this can't prove the content is the same, and the note says *looks to be*.
4. **Only override mods.** Add-ins live in their own folders and stay copies by UID alone (ADR-0039).
5. **An archive's names are read once.** Library mods never change, so a reload doesn't read them again.

## Consequences
- A player sees why two mods overlap and can clear one, and a small mod they prefer stays unless they press the button.
- A mod with two of these notes names only the biggest, since keeping that one covers the rest.
- A fragment of another name stays until the player acts, even if they would never want it.

## Alternatives considered
- **Count these as parts, so *Keep one of each* offers them.** Rejected by ADR-0039 for the same reason: it starts on
  removing what may be the mod the player chose, and removing can't be undone.
- **Hash each resource inside an archive.** Would prove it, at the price of reading every archive whole on every new mod.
  The note's *looks to be* is enough for an offer the player reads first.
