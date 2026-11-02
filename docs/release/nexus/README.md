# Nexus Mods page, ready to paste

This page describes the six-tab window ([ADR-0026](../../adr/0026-workflow-first-layout.md): Home, Mods, Fixes, Saves,
History, Settings) and its plain words (*Turn on*, *Manage*, *Undo*). **Paste it when the first release with that
window is on Nexus**; until then the live page keeps describing 0.3.2. Its pictures are in their own folder, so
merging this changes nothing on the live page.

| Nexus field | Paste from |
|---|---|
| Mod name | `DAORework - Mod Manager, Launcher and Diagnostics` |
| Brief overview (summary) | [summary.txt](summary.txt): plain text, no formatting, under Nexus's 350 characters |
| Detailed description | [description.bbcode](description.bbcode): open it, select all, copy, and paste into the description editor in BBCode mode (the `[BBCode]` toggle) |
| Permissions and credits | [permissions.md](permissions.md): which option to pick for each permission, and the text for Author notes and File credits |

## Pictures

The `[img]` lines in the description load the pictures from the project website,
`https://daorework.dilly-world.net/images/nexus/` ([ADR-0021](../../adr/0021-project-website.md)), so they show on
Nexus as soon as the website has them (it rebuilds when `main` changes).

Also upload them to the mod's **Images** tab, so they appear in its gallery: the files in
[site/public/images/nexus](../../../site/public/images/nexus), `00-cover.png` as the main image, then the screenshots
in order, each with its line from [captions.txt](captions.txt). Remove the 0.3.x pictures from the gallery at the same
time; they show the old tabs. (Or use your own screenshots from a real install, which show Windows paths, real mod
names and a real antivirus result.)

Where the pictures come from:
- `01` to `08` are the App tests' headless screenshots (`dotnet test tests/DAORework.App.Tests`, with
  `DAOREWORK_SCREENSHOT_DIR` set to a folder), with made-up mods: `21-next-steps`, `32-mods-one-page`,
  `28-adopt-selected`, `30-chargenmorph-combined`, `17-load-order`, `19-fixes-undone`, `15-changes-rollback` and
  `24-look-with-picture`. Taken on Linux, so a mod's antivirus line reads *Not scanned*, which is what DAORework says
  where the Windows scan is not available.
- `00-cover.png` is [cover.html](cover.html) at 1920x1080, built from `01-home.png` and `02-mods.png`.

When P2.5-28 (the gilded-tome style) lands, the window changes look again: retake `01` to `08` from the same tests and
the cover from `cover.html`.

## On upload day

Also choose: the category (Utilities), the generative-AI tag by Nexus's rules that day, and the file: the same zip as
the GitHub release (the release workflow uploads it by itself once set up, [nexus-upload.md](../nexus-upload.md)).
[nexus-page-draft.md](../nexus-page-draft.md) keeps the notes behind these choices.
