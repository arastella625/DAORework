# ADR-0016: GUI themes, with Witch of the Wilds as the default

- **Status:** Accepted
- **Date:** 2026-10-29

## Context
`DAORework.App` uses Avalonia's Fluent theme as it comes, in the system's light or dark variant. The views set a few
colors themselves (`#C42B1C` for errors in `ModsPage.axaml`, the two brushes in `StatusBrushes`), and the rest is
Fluent's grey. Phase 2.5 adds most of the GUI (P2.5-02/03, P3-04, P2.5-05 to P2.5-07) ahead of the first public
release, so the look has to be settled before those pages are built, not reskinned after.

The owner wants the app to read as Dragon Age: Origins and to be personal: a mage, "Morrigan style". Three directions
were mocked on the Mods, Diagnostics and confirm screens: **A** "Witch of the Wilds" (near-black violet and plum,
arcane violet actions, verdigris green, tarnished gold), **B** "The Fade" (cooler, lavender, a glow on anything
interactive) and **C** "Blood and Shadow" (iron, leather and crimson). The recommended blend is A as the base, B's
glow for focus, hover and selection, and C's red only for danger. Checking the mocks against WCAG AA showed that
several starter colors fail as text (the violet, the red and the muted gold) and had to be split into a fill color and
a text color.

The SDD says nothing about theming, and LEGAL_BOUNDARIES rules out anything taken from the game's own UI.

## Decision

### Four themes, one set of tokens
- The app ships four themes. **Witch of the Wilds** (the blend) is the default. The user can pick one of the other
  three instead: **Korcari** (A as sketched, with a plain focus outline and no glow), **The Fade** (B) and **Blood and
  Shadow** (C). All four are dark. A light variant is not part of this decision.
- Every theme defines the same **tokens**: named colors, brushes and a few numbers. A view never names a color. It
  uses a token through `{DynamicResource ...}`, so switching theme restyles every open page without reloading it.
- Tokens are named by role, not by color: a brush is `<Token>Brush` and its color `<Token>Color`.

| Token | Role | Witch of the Wilds |
|---|---|---|
| `Background` | The window | `#120D18` |
| `Surface` | Panels, the header, cards' ground | `#1E1528` |
| `SurfaceRaised` | Cards, dialogs | `#2A1E38` |
| `Border` | Dividers and filigree. Decorative, no contrast minimum | `#6B5A3A` |
| `BorderStrong` | Input and button outlines (3:1) | `#8C7650` |
| `TextPrimary` | Body text | `#E8DFCF` |
| `TextSecondary` | Paths, hints, secondary lines | `#A99BB8` |
| `Heading` | Page, section and dialog titles | `#C9A96E` |
| `Accent` | Fill of the primary button (*Launch game*, *Adopt*) | `#6D45C4` |
| `OnAccent` | Text on `Accent` | `#E8DFCF` |
| `AccentText` | Links, the selected tab, Info | `#A98BFA` |
| `AccentSecondary` | Witch-green fill (badges) | `#4F8A6E` |
| `AccentSecondaryText` | Green text and icons | `#6FB394` |
| `Success` | Done, passed | `#86BC72` |
| `Warning` | Warning text and icons | `#C9A227` |
| `Danger` | Fill of a destructive button | `#9E2B2B` |
| `DangerText` | Error text and icons | `#E57373` |
| `FocusGlow` | Focus ring, hover and selection glow | `#B9A3FF` |
| `GlowRadius` | Blur of the glow, in pixels (`0` turns it off) | `16` |
| `Emphasis` | A rare gradient: the selected tab's underline and a dialog's title rule | violet to crimson |

*2026-10-29: `DangerText` was lightened from `#E06C6C` to `#E57373`, so error text also passes AA where the
page's smoke is strongest.*

The other three themes differ from the default as follows. Their exact values live in their own dictionaries and
pass the same tests.
- **Korcari:** the default's colors, `GlowRadius` `0`, and a plain `Border` gold `Emphasis`.
- **The Fade:** cooler surfaces (`#13121E`, `#1B1A2B`, `#25233A`), lavender headings and text, `Accent` `#5E4BB8`, a
  pale teal second accent, a rose `Danger`, and the strongest glow.
- **Blood and Shadow:** iron and leather surfaces (`#100B0D`, `#1A1214`, `#251A1D`), a bronze `Accent` (`#6E5130`),
  and crimson kept for `Emphasis` and `Danger`. Its accent is deliberately not red. In the mock where it was, a
  destructive button looked like every other button and Info looked like Error.

### Rules every theme and every view follows
1. **No color outside a theme.** Views, styles and converters use tokens only. `StatusBrushes` and the hard-coded
   reds in the views move to tokens in the first implementation PR.
2. **AA contrast in every theme:** 4.5:1 for text, 3:1 for headings (they are large text), control outlines and the
   focus ring. The pairs checked are text on each surface, `OnAccent` on `Accent`, `TextPrimary` on `Danger`, each
   status color on `SurfaceRaised`, and `BorderStrong` and `FocusGlow` on `Surface`.
3. **Red means danger.** `Accent` is never a red: its hue lies outside 330° to 20°. A filled red button is only ever
   a destructive confirm, such as *Roll back*. Adopting, activating and applying a fix are backed up and undone by a
   rollback, so their confirm uses `Accent`.
4. **Never color alone.** Every severity and plan step shows an icon and a word as well as a color: Error, Warning and
   Info in Diagnostics, blockers and warnings in a plan.
5. **Drawn by us.** Motifs are our own vectors and styles: thin rune dividers under page titles, corner filigree on
   cards, a faint vellum grain, a violet glow behind the page, and a raven-feather mark as the app's emblem. Nothing is
   copied or traced from the game, BioWare's or EA's art, logos or the Grey Warden griffon (LEGAL_BOUNDARIES).
6. **Fonts.** Headings use **Cinzel** (SIL Open Font License 1.1), embedded in the App as an Avalonia resource, with
   its license in `docs/licenses/` and a row in `THIRD-PARTY-NOTICES.md`. Cinzel is set only on titles. Body text,
   names the user typed, paths and codes stay in Inter, as shipped today by `Avalonia.Fonts.Inter`, and a monospace
   face.

### How it is built
- Each theme is one `ResourceDictionary` of tokens in `src/DAORework.App/Themes/`, one file per theme. It is
  registered in `Application.Resources.ThemeDictionaries` under its own `ThemeVariant` (for example
  `new ThemeVariant("WitchOfTheWilds", ThemeVariant.Dark)`), so Fluent's own controls keep their dark resources
  underneath. Switching theme sets `Application.RequestedThemeVariant`.
- The same dictionaries map Fluent's accent and control resources (`SystemAccentColor`, the button, tab and combo box
  brushes) to tokens, so a control without a DAORework style still matches.
- Shared styles (`Button.accent`, `Button.danger`, page titles, cards, severity rows, the focus glow) live in
  `Themes/Styles/`, split by control so each file stays under the 300-line limit. View models do not know about
  themes beyond the picker's list (SDD §9.2).

### Choosing a theme
- A new **Settings** page holds the theme picker: the four themes as cards with their name, one line and a strip of
  their colors. Picking one applies it at once and saves it.
- The choice is saved in `preferences.json` in the app-data root (SDD §8.4), read and written through an
  `IPreferencesStore` port with the JSON adapter in Infrastructure and use cases in an Application slice. The App
  owns the list of theme ids. A theme is the base of a *look*, and a launch profile can have its own look
  ([ADR-0017](0017-custom-looks.md)), which also sets the file's fields.
- A missing file means the default. A file that cannot be read, or an id the App does not know (a theme removed in a
  later version), also falls back to the default, with a `Warning` in the log. The next pick overwrites it atomically.
- The CLI has no theme command. The setting has no effect there.

## Consequences
- Every new Phase 2.5 page is built on tokens from the start, and its screenshots show the real look.
- `DAORework.App.Tests` gains three checks: every theme defines every token, every pair in rule 2 passes in every
  theme, and no `.axaml` or `.cs` file under `Views/` contains a color literal. It renders every page in the default
  theme and the Mods page in each of the other three, and those screenshots go on GUI PRs as today.
- Four themes are four sets of values to keep correct. The tests above are what make that cheap. A fifth theme, or a
  light variant, is a new dictionary that passes the same tests, plus an ADR if it changes a rule.
- Glow and gradients cost a little rendering time. `GlowRadius` `0` (Korcari) is also the choice for anyone who finds
  the glow distracting.
- The app no longer follows the system's light mode. That is a deliberate change from today and goes in the
  changelog.
- The work ships as separate PRs: this ADR and the roadmap task; the tokens, the four dictionaries, the shared styles
  and the move of the existing views onto them; the preference store with its use cases; the Settings page; Cinzel
  and its license.

## Alternatives considered
- *Keep Fluent's default look:* clean, but generic, and not what the owner asked for.
- *One theme only (the blend):* simpler, but some players dislike glow or would rather have the classic red-and-iron
  look, and each extra theme costs one tested dictionary.
- *Hard-coded colors per view, tidied later:* every Phase 2.5 page would have to be reskinned, and nothing would stop
  a failing contrast pair.
- *A light variant now:* the brief is dark, and each light theme would double the values to check. It can come later.
- *Keep C's crimson accent:* faithful to the mock, but red would no longer mean danger.
- *Cormorant Garamond for headings:* also OFL and also tried in the mocks. Cinzel's capitals read more like the
  game's world at heading size.
