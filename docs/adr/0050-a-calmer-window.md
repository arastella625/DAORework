# ADR-0050: A calmer window

- **Status:** Proposed
- **Date:** 2026-11-15
- **Amends:** [ADR-0026](0026-workflow-first-layout.md) §1 (Home and Mods) and [ADR-0038](0038-rows-a-sheet-and-drag.md)
  (what a row shows and where its details go)

## Context
On 2026-11-15 the owner said the window "isn't much for people yet". The screenshots in the README show why, against
[VOICE.md](../VOICE.md)'s own rules:
- **Home has no one answer.** A banner says four mods can't be turned off, a card says *Not checked yet*, another says
  *Almost there*, and three tiles under it each say *Ready*. VOICE rule 6 asks for one main button a screen; Home has
  four.
- **Rows show plumbing.** Nearly every row on Mods reads *override mod · loses*. *Loses* is true and means a texture
  of another mod shows instead, which is usually what the player wanted, but the row offers nothing to do about it.
  Add-ins now add *Priority 109*.
- **The payoff is hidden.** Combining `chargenmorphcfg.xml` ([ADR-0025](0025-combining-chargenmorphcfg.md)) is what
  makes hair and face mods work together, and the window says only *Combined files · None needed* or *Ready*.
- **A name doesn't match.** The list called *Combined files* mostly lists files where one mod's copy wins: 372 of them
  only change how things look.

The six tabs, the plain words, the gilded-tome style and previews before every change stay as ADR-0026 has them.

## Decision
1. **Home: one headline, one next step.** Home holds one card, *Last change · Undo*, and *Worth a look* (at most
   three, as now).
   - The card has a headline, one sentence and one button. One Core rule decides all three, from what `NextMoves`
     weighs today and from the last check of the game.
   - Headlines: *Welcome* (no game yet), *One thing first* (a step the player should take, such as *Take them over*
     or *Turn them on*), *Not ready yet* (the check found something that stops the game), *Ready to play* and
     *Ready to play, without your mods*.
   - The game is checked when the window opens and after each change, so *Not checked yet* goes, and *Check again*
     stays in Settings › Troubleshooting.
   - The three kind tiles leave Home; they stay on Mods, where they filter. *New downloads* and *Find the problem mod*
     come to Home as *Worth a look* steps, when they apply.
2. **A row: the picture, the name, what it adds, the switch.**
   - A row shows a small thumbnail ([ADR-0049](0049-pictures-and-words-a-mod-brings.md)), the mod's name, what it adds
     (§4) when known, a warning when there is one, *Could break quests* when it does, and its switch.
   - The kind (*Override mod*, *Add-in*, *Runs in the game*), *wins / loses*, Priority, copies and sizes leave the
     row. They are under *Details* on the mod's page (§3), on *Where mods overlap* (§5) and in *Arrange*.
   - The message after turning several mods on is one line (*Turned on 59 mods. 3 need a look.*) and *Details*.
3. **About this mod.** Clicking a row opens the mod's page, in the Mods tab in place of the list, with *Back to your
   mods* returning to the same place in the list. It replaces ADR-0038's expand in place.
   - It shows the lead picture large, then the others, the description and author, what the mod adds, its switch, its
     warnings, and *Open the mod's page* when its page is known.
   - *Details* holds what the row used to: kind, files and size, where it came from, the antivirus verdict, *wins /
     loses* and Priority, and the copy and *Keep this one* notes.
   - A mod with no picture shows a crest drawn in the theme's colours, by kind. No new third-party art.
4. **What you got.** Where a mod adds to character creation, the row and its page say so in counts:
   *Adds 12 hairstyles*, *Adds 3 faces and 8 colours*. After turning mods on, the message says what character creation
   gained: *Character creation has 48 more hairstyles*. Where nothing can be counted: *Adds to character creation*.
   Counts are never guessed: they come from the mods' own `chargenmorphcfg.xml` files, compared with the game's copy on
   the player's PC where it can be read, else with what every mod's copy shares.
5. **Cards, and where mods overlap.**
   - *Your mods* gets a *List | Cards* switch, remembered for the player. Cards show the lead picture, the name, what
     it adds and the switch, with the same search, sort and filters. Ticking several mods is in *List* only.
   - The *Combined files* card and list become *Where mods overlap*. *Could break quests* comes first, then
     *Combined, so both work* (with what character creation gained), then *Only looks*, folded to one line with
     *Show*.

## Consequences
- Home answers *can I play?* and *what now?* in one place, and the readiness strip's detail lives only on Mods.
- Checking on open runs in the background, which on a large install may take a while. The card says *Checking your
  game* until it's done, and nothing waits on it.
- A row no longer says *loses*. A player who wants to know who wins opens *Where mods overlap* or *Arrange*, which is
  where they can change it.
- The mod's page replaces expanding a row. Comparing two mods' details side by side needs two clicks, where it used to
  need two expands.
- *Combined files* is in released screenshots, the website and the Nexus page, so P2.10-07 retakes them.
- VOICE.md gains the new words, and *Combined files* changes there.

## Alternatives considered
- *Keep Home's cards and add a summary line on top:* one more thing to read, and the four buttons stay.
- *Keep the kind and wins/loses on the row in a quieter colour:* still plumbing to a player who didn't ask, and still
  no action on the row.
- *The mod's page as a separate window or a sheet:* a second window breaks [ADR-0044](0044-one-window-at-a-time.md)'s
  spirit and hides the list; a sheet is ADR-0038's preview's place.
- *Cards only:* a list of 70 mods with ticking several at once is still the fastest way to manage many.
