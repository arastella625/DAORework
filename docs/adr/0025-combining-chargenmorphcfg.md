# ADR-0025: Combine chargenmorphcfg.xml from every active mod

- **Status:** Accepted
- **Date:** 2026-11-01

## Context
`chargenmorphcfg.xml` lists what the character creator offers: head presets, hairs, beards, colours, tattoos and
skins ([toolset wiki](http://www.datoolset.net/wiki/Chargenmorphcfg.xml)). The game reads one copy from the override
folder and ignores the rest. Hair, head and colour mods each ship their own copy, usually the whole base list plus
their additions, so with two of them active only one mod's additions reach the character creator. Players merge the
copies by hand or with community tools (CharGenMorph Compiler, CharGenMorphCfg Generator).

DAORework treated the file like any other: the copy from the mod last in the load order was deployed and the plan
warned that the others' copies were not used. The owner asked for the copies to be combined (2026-11-01). DAORework
has only deployed files a mod ships; a file it writes itself is new (ADR-0012), hence this ADR.

The format: a `<morph_config>` root; sections such as `heads`, `hairs`, `beards`, the colour lists, `tattoos` and
`skins`; in most sections, groups by race and gender (`human_male`, `elf_female`, ...); and `<resource>` entries with
a `name` (hairs also a `cut`; the same hair with two cuts is two entries the game shows).

## Decision
- **When two or more active mods ship `chargenmorphcfg.xml`** (in any folder of their override files, any letter
  case), the plan deploys none of their copies. It deploys one combined file instead, at
  `packages/core/override/DAORework/_combined/chargenmorphcfg.xml`. A mod id always ends in a hash, so `_combined`
  can never be a mod's folder. With one such mod, its copy is deployed unchanged, as before.
- **How copies combine**, in load order (lowest priority first): the first copy gives the order; each later copy adds
  the sections, groups and entries the combined file does not have yet, after those already there. An entry is the
  same as another when its element name and all its attributes match (names compared without letter case), so the
  same hair with another `cut` is kept. Entries are never removed or reordered. Elements and attributes DAORework does
  not know are kept as they are.
- **A copy that cannot be read is left out:** a copy that is not well-formed XML, or whose root is not `morph_config`,
  is left out of the combined file and named in a warning; the others are still combined. The game could not have read
  it either, so nothing is guessed. Only when no copy can be read is nothing combined, and the copy from the mod last in
  the load order is used, as before. *(Amended 2026-11-01 after a real run: falling back to the last copy installed the
  broken one when it was last, though good copies were there.)*
- **Owned like any deployed file:** the combined file is kept in the library under `library/_combined/<sha256>/`,
  never overwritten once written, and deployed from there like any mod file. It is in the deployment record under the
  reserved id `_combined`, backed up before it is written or removed, and rolled back with the change. Each plan
  works it out again from the active mods; the output is deterministic, so an unchanged set of mods changes nothing.
  When fewer than two such mods remain active, the plan removes it. Activating, deactivating, reordering and adopting
  all combine.
- **The plan says so:** *Combines chargenmorphcfg.xml from Thedas Hairs, More Eyes and Beautiful People: 214
  entries.* The CLI's text says the same.
- **The user's own copy** (a `chargenmorphcfg.xml` in the override folder that DAORework did not place) is never
  changed. The plan warns that the game picks one copy, as it does for any shadowed file; adopting it (ADR-0015) makes
  it one of the combined copies.
- **Only the mods' files are read.** The base game's copy inside its own archives is neither read nor written, and
  nothing is downloaded.

## Consequences
- Hair, head and colour mods work together without hand-merging, and the combined file goes away with the mods.
- A mod that ships only its additions (not the base list) behaves as it did before: whatever the combined copies list
  is what the character creator shows.
- The planner gains its first generated file, so ownership checks, backups and rollback are tested with one.
- A malformed copy keeps the old behaviour and is named, so a broken mod file never breaks the character creator.

## Alternatives considered
- *Keep warning only:* leaves players to merge by hand, the chore this project exists to remove.
- *Let the player pick which copy wins:* still loses every other mod's additions.
- *Generate the file from resource names on disk (like the CharGenMorphCfg Generator):* guesses from file name
  patterns and needs the base game's list from its archives.
- *Combine in place in the override folder's root:* would overwrite a copy the user placed by hand.
