# ADR-0017: Custom looks: background pictures, accent colors and a look per profile

- **Status:** Accepted
- **Date:** 2026-10-29

## Context
ADR-0016 gives the app four built-in themes. The owner also wants the app to be theirs: a picture of their own as the
window's background, blended into the theme, an accent color they choose, and a different look for each playthrough.
They chose on 2026-10-29 that a look belongs to a launch profile, with an app-wide look for profiles that have none,
and that this lands in Phase 2.5 (P2.5-11). A music player for the user's own files was also asked for. It waits for
a later phase (P3-05) because it needs an audio library, which is its own ADR under ADR-0009.

Three things constrain the design:
- ADR-0016's rules still hold for any picture and any color: AA contrast for all text, and red means danger.
- DAORework ships no pictures. A background is always a file the user picked, and nothing is downloaded
  (LEGAL_BOUNDARIES, SDD §12).
- File access stays in Infrastructure, and a profile's launch settings stay free of presentation details.

## Decision

### A look
- A **look** is a base theme (one of ADR-0016's four), an optional accent color and an optional background picture,
  under a name the user gives. The four themes are built-in looks: they cannot be changed or deleted, and
  *Customize* makes an editable copy.
- A custom look is stored in app-data as `looks\<lookId>\look.json` (`schemaVersion` 1: the name, the base theme, the
  accent as `#RRGGBB` or `null`, and the background's file name, fit, tint, dim and blur), next to its picture,
  `looks\<lookId>\background.png` or `.jpg`.

### Which look is shown
- `preferences.json` (ADR-0016) holds the app-wide look and the profile looks:
  `{ "schemaVersion": 1, "look": "witch-of-the-wilds", "profileLooks": { "Wilds run": "<lookId>" } }`.
  The window shows the selected profile's look, or the app-wide look when the profile has none. Switching profile
  switches the look. (Chosen by the owner, 2026-10-29.)
- The Profile page gains a *Look* choice: *App default (name)* or any look. The Settings page lists every look, sets
  the app-wide one, and opens the editor.
- A profile look that names a deleted look falls back to the app-wide look, and the entry is dropped at the next
  save. An entry for a profile that no longer exists is ignored. Profiles are keyed by name, so a future rename use
  case has to move the entry too.
- A look is not stored in `RuntimeProfile`. The profile stays a launch configuration, and its schema does not change.

### Background pictures
- The user picks a PNG, JPEG or WebP file. Importing decodes it and refuses a file that cannot be decoded
  (`DAO_LOOK_IMAGE_UNREADABLE`) or is over 50 MB (`DAO_LOOK_IMAGE_TOO_LARGE`). It scales it down so its long edge
  is at most 3840 px and saves the copy in the look's folder. The original file is never changed, moved or read
  again.
- The window is drawn in layers, bottom to top: the theme's `Background` color; the picture, filled to the window
  (or fitted), blurred by up to 24 px; a **tint** that maps the picture's brightness onto the theme, from `Background`
  to `Accent`, by the chosen amount; a **veil** of `Background` at the chosen dim; ADR-0016's smoke and grain; and the
  page. Cards, dialogs and the header stay opaque (`Surface`, `SurfaceRaised`).
- **Text stays readable over any picture.** When a look's picture, tint, blur, base theme or accent changes, the app
  measures the brightest part of the treated picture (the 98th percentile of its luminance). It then computes the
  lowest veil at which every text token passes ADR-0016's contrast rule over it. The dim slider cannot go below
  that floor, and the editor says why. In the default theme, a picture whose bright parts are mid grey (`#808080`)
  needs a 72% veil, and one no brighter than `#404040` needs 35%. (These were 77% and 47% before ADR-0016
  lightened `DangerText`; `VeilFloorTests` checks them.) Dark, low-contrast pictures show the most. A
  bright one is shown faintly rather than making text hard to read.
- The treated picture is rendered once, when the look or the window size changes, not on every frame.

### Accent color
- The user picks any color. The app keeps its hue and derives `Accent`, `AccentText`, `OnAccent`, `FocusGlow` and
  the start of `Emphasis` by changing only lightness, until each passes ADR-0016's contrast rule. The editor shows
  the result and says when it lightened or darkened the pick.
- A red hue (330° to 20°) is refused for the accent (`DAO_LOOK_ACCENT_RESERVED`), because red means danger (ADR-0016
  rule 3). No other token can be edited. A look that wants more than this is a new built-in theme.

### Where the code goes
- **Core** `Looks/`: the `Look` record, a color value, and the contrast, veil-floor and accent-derivation rules.
  They are pure and need no images: the importer passes in the measured luminance.
- **Application** `Looks/`: one use case each for creating, updating, deleting and listing looks, importing a
  picture, and setting the app-wide and a profile's look. The ports are `ILookStore` and `IBackgroundImageImporter`.
  Expected failures return `Result<T>` with the `DAO_LOOK_*` codes in `DiagnosticCodes`.
- **Infrastructure**: the JSON look store (atomic writes, SDD §8.4), and the importer, which decodes, measures and
  scales with SkiaSharp. SkiaSharp already ships in every release through `Avalonia.Skia`. The direct reference is
  justified in its PR.
- **App**: the Settings *Looks* section, the look editor with a live preview, the Profile page's choice, and the
  layered window background. View models hold only the editor's state.

### Privacy and legal
- Looks and their pictures live only in app-data. Support bundles never include a picture or a look's name, only
  whether a look has a background.
- DAORework ships no background pictures and offers none for download. A screenshot the user took of their own
  game is their own file on their own machine, and DAORework only copies it within their app-data.

## Consequences
- Every player can make the app theirs without a new theme being written, and the contrast rules still hold.
- A bright picture shows only faintly. That is the price of readable text, and the editor explains it.
- New tests: the veil floor holds over synthetic pictures (white, black, gradients, noise), every hue from 0° to
  359° either derives an accent that passes or is refused as red, `look.json` round-trips, and an unreadable or
  oversized file is refused. The screenshot tests use a picture the test generates, so no artwork is committed.
- A decoded 3840 px picture takes about 33 MB of memory while it is shown. Only the shown look's picture is
  decoded.
- `preferences.json` is introduced here at schema 1 with both fields, so it needs no migrator yet.
- The work ships as separate PRs, after ADR-0016's: the Core rules; the look store and the use cases; the picture
  importer; the editor and the Settings and Profile page changes.

## Alternatives considered
- *Point at the original picture instead of copying it:* moving or deleting it would break the look, and DAORework
  would read outside app-data on every start.
- *A free veil slider with a warning:* readable text would depend on the user's taste, and ADR-0016 rule 2 would
  no longer hold.
- *Translucent cards over the picture:* the picture would show more, but every card would need the same veil, and
  text on a card would depend on the picture.
- *Edit every token:* no contrast guarantee, and red could stop meaning danger.
- *Store the look in `RuntimeProfile`:* the launch configuration would carry presentation details and need schema 2
  with a migrator.
- *One app-wide look only:* simpler, but the owner wants a look per playthrough.
