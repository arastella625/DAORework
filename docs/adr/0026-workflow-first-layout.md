# ADR-0026: A workflow-first layout, plain words and the gilded-tome style

- **Status:** Accepted
- **Date:** 2026-11-01

## Context
The window grew one panel per feature: 6 tabs, with 5 more inside Mods, so 11 places to look. It opens on four file
paths that are set once. Rows show internal ids (`extra-dog-slot-ad3aa231`), sizes like "8 B", diagnostic codes and
UTC timestamps. Nearly every panel repeats "nothing changes until you apply it; a backup is taken first", and success
messages stay on screen after they stop meaning anything. Players coming from Vortex, Mod Organizer or the manual
setup should feel at home in seconds, and see why DAORework is different: every change can be undone, a hand-built
setup comes in without reinstalling, every mod is scanned, and character-creator files are combined.

On 2026-11-01 the owner reviewed a clickable mock (six tabs, plain words, a gilded style) and chose:
- the six-tab layout;
- the plainer words (*Turn on*, *Manage*, *Undo*);
- mods told apart by how the game loads them (add-ins, override mods, files several mods need at once), with one
  view that says the whole setup is ready;
- a richer, on-theme look for buttons and frames, with free third-party art where its licence allows.

The owner left the style direction open. Direction A, the *gilded tome*, was taken because it suits the art she
supplied; changing it later is a change of templates, not of layout.

## Decision

### 1. Six tabs, none inside another
| Tab | Holds |
|---|---|
| **Home** (opens first) | Whether the game is ready to play; a readiness strip for the three kinds of mod; the last change with *Undo*; at most three suggestions ("Worth a look"), each with one button that goes where it is done. |
| **Mods** | One list of every mod, each with an on/off switch. Three cards across the top (Add-ins, Override mods, Combined files) say how many are on and whether each part is ready, and filter the list. Mods installed by hand are in the same list, tagged *Found in your game*, with *Manage*. *Arrange* turns the list into the load order. A change opens a short preview (*Apply*, *Cancel*, *Details*). |
| **Fixes** | DXVK and the 4 GB patch, each a card with its state and one action. |
| **Saves** | *Back up now*, how many backups to keep, and the backups with local times and *Show in folder*. |
| **History** | Every change DAORework made, from Mods and Fixes, newest first, each with *Undo* (the newest) or *Undo to here*, confirmed first. |
| **Settings** | *Look* (themes and the user's own looks), *Game folders* (the profile's paths, closed by default), *Troubleshooting* (a one-line summary, *Check again*, *Copy details for a bug report*, the support bundle, and the full report behind a button). |

- The header holds the profile switcher (with *New profile…* inside it) and the one large button, *Play*.
- A footer line says *Every change can be undone* and links to History. It replaces the sentence repeated on every
  panel.
- **The Combined files card** lists each file more than one mod ships, with what happened to it: *Combined* (merged,
  as `chargenmorphcfg.xml` is, ADR-0025), *One wins* (with *Arrange*), or *Needs a patch* (only a compatibility patch
  from the mods' authors joins them, as P2.5-21 explains for `.gda` tables and scripts).

### 2. Room to grow, without a seventh tab
- A new kind of mod (the install folder's own override, P2.5-22; add-in priority, P2.5-23) becomes a tag and a filter
  in Mods, and a tile on Home if players need to see it at a glance. It does not become a tab.
- A new way of combining files (P3-07 to P3-09) adds rows and states to Combined files.
- A new check adds a suggestion on Home. Home shows the three most important; the rest are under Troubleshooting.
- A new kind of change (save restores, a music setting) adds entries to History in the same shape.
- A new preference adds a section to Settings. The music player (P3-05) goes there, with a small control in the
  header.
- A seventh top-level tab needs this ADR amended.

### 3. Plain words ([VOICE.md](../VOICE.md))
- The window's words follow `docs/VOICE.md`: talk about the player's game, not the code; details only when asked;
  each reassurance once; messages that fade; numbers and times as a person writes them; one main button per screen.
- The window's verbs change: *Activate* → *Turn on*, *Deactivate* → *Turn off*, *Adopt* → *Manage*, *Roll back* →
  *Undo*, *Installed by hand* → *Found in your game*, *Load order* → *Arrange*, *plan* → *preview*, *Changes* → *History*.
- The command line keeps its words (`mods activate`, `mods adopt`, `mods rollback`): scripts and existing guides use
  them. The CLI's help text mentions the window's words where they differ.
- Diagnostic codes are unchanged and never renamed (SDD §8). The window shows them only in details and in *Copy
  details for a bug report*.

### 4. The gilded-tome style
- Buttons: the main button is violet with a gold edge and an inner dark rule, its label in small capitals (Cinzel);
  other buttons are dark with a gold hairline; a quiet button is an underlined link; *Delete* and other destructive
  confirms stay the only red (ADR-0016 rule 3).
- Frames: cards have a double rule (an outer gold line and a faint inner one); the cards on Home use stretchable
  borders from Kenney's *Fantasy UI Borders*; page titles keep the rune divider.
- Switches have a gem knob that glows when on; readiness shows as a seal (green tick, amber when something needs a
  look); a soft sunburst sits behind *Ready to play*.
- Every ornament is a token colour. Vector ornaments are drawn as `Path` geometry; bitmap ornaments are white masks
  painted with a token brush (`OpacityMask`), so every theme and custom look (ADR-0017) recolours them. *Korcari* has
  no ornaments, as it has no glow today.
- ADR-0016's rules still hold: AA contrast for text, 3:1 for controls and focus, red only for danger.

### 5. Third-party art
- Art shipped with DAORework is CC0 or CC-BY, or the author has given written permission to include it in this
  public, noncommercial repository and its releases; that permission is kept in `docs/licenses/`.
- Art whose licence forbids redistributing the files (most itch.io packs say "can't distribute", "may not be made
  available ... as standalone files") is never committed, even when it was bought: the repository and the release
  zip both let anyone take the files. It may inform our own drawings; it is never traced.
- Nothing from BioWare's games, and nothing whose licence forbids AI-assisted projects.
- Each pack is listed in `THIRD-PARTY-NOTICES.md` with its source, licence and use, and its licence file sits beside
  it under `src/DAORework.App/Assets/`.
- The first pack is Kenney's *Fantasy UI Borders* 1.0 (CC0).

### 6. Built in steps, each its own PR with screenshots
1. The words (P2.5-24): `VOICE.md` applied to every view and message; no layout change.
2. Home (P2.5-25), with the readiness strip and suggestions.
3. Mods as one list with the three cards and Combined files (P2.5-26).
4. History, and Settings taking the game folders and diagnostics (P2.5-27).
5. The gilded-tome templates and art (P2.5-28).

## Consequences
- Every page's screenshot changes, and the README, Nexus page and website screenshots are retaken after step 5.
- The Mods tab's sections (P2.5-17) are replaced by one list and three cards. Work on that tab (P2.5-21 to P2.5-23)
  lands into the new layout from step 3 on, so the two streams agree on the order before step 3 starts.
- Players who learned *Adopt* and *Roll back* from 0.3.x see new words; the release notes list them.
- `ViewsUseTokensTests` extends to the new templates: no colour names in views, ornaments included.
- Bitmap ornaments add a few kilobytes to the App; vector ornaments add none.

## Alternatives considered
- **Only rewording the current tabs:** the 11 places and the setup-first opening stay.
- **A tab for Combined files:** it describes the same mods as Mods does; splitting them makes people look in two places.
- **Direction B, a grimoire of the Wilds** (round, vines, moon switches): kept for a future theme, not the default.
- **The bought packs (Selfmaiden, dobo_ui, Hypnobius):** their licences forbid redistribution. The Selfmaiden frame
  can be added if its author gives written permission.
- **Pixel-art packs as the main look:** they look blocky beside sharp text; at most an accent.
