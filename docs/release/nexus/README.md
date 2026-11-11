# Nexus Mods page

The page's summary and description live here, and the **Nexus page** workflow sends them to Nexus whenever they change
on `main` ([ADR-0030](../../adr/0030-nexus-page-text-from-the-repository.md)). To change the page's text, change these
files by PR; an edit made on the Nexus website is overwritten by the next change here. The name, the gallery,
permissions and the category stay manual.

| Nexus field | Where it comes from |
|---|---|
| Mod name | `DAORework - Mod Manager, Launcher and Diagnostics` (by hand) |
| Brief overview (summary) | [summary.txt](summary.txt): plain text, no formatting, under Nexus's 350 characters (sent by the workflow) |
| Detailed description | [description.bbcode](description.bbcode): BBCode (sent by the workflow; by hand, paste it in the editor's `[BBCode]` mode) |
| Permissions and credits | [permissions.md](permissions.md): which option to pick for each permission, and the text for Author notes and File credits (by hand) |

## Setting up the page sync (once)

1. **On GitHub:** **Settings** → **Environments** → **New environment** `nexus-page`. Under **Deployment branches and
   tags**, choose **Selected branches and tags** and add `main` only.
2. In `nexus-page`, add the secret `NEXUS_API_KEY` (the same personal key as the `release` environment's, from
   [your API keys](https://www.nexusmods.com/settings/api-keys)) and `NEXUS_MOD_ID` = `601295428523` (a variable or a secret), the
   Unique Mod ID ([nexus-upload.md](../nexus-upload.md)).
3. **Check the files match what you want live**, since the first run replaces the page's summary and description.
   Then run it: **Actions** → **Nexus page** → **Run workflow** on `main`. The run's summary says what was sent; open
   the mod page to check it.

If a run fails, its log shows Nexus's answer. The API call is experimental (ADR-0030): if Nexus changes it, paste by
hand as in the table above until the workflow is fixed. If the key leaks, revoke it on Nexus and put a new one in both
environments.

## Pictures

The `[img]` lines in the description load the pictures from the project website,
`https://daorework.dilly-world.net/images/nexus/` ([ADR-0021](../../adr/0021-project-website.md)), so they show on
Nexus as soon as the website has them (it rebuilds when `main` changes).

Also upload them to the mod's **Images** tab, so they appear in its gallery: the files in
[site/public/images/nexus](../../../site/public/images/nexus), `00-cover.png` as the main image, then the screenshots
in order, each with its line from [captions.txt](captions.txt). Replace the gallery's pictures at the same time, so it
shows the window that can be downloaded. (Or use your own screenshots from a real install, which show Windows paths,
real mod names and a real antivirus result.)

Where the pictures come from (retaken for 0.6.0, all from the App tests with made-up mods):
- `01` to `07`, `11` and `12` are copies of the README's pictures (`docs/screenshots/readme/`), mostly the real-scale
  library: `01-next-steps`, `02-library`, `05-adopt-plan`, `03-activate-plan`, `04-load-order`, `06-fixes`,
  `07-rollback`, `10-combined-files` and `11-problem-search`. Copy them again when those are retaken.
- `08-your-look.png` is the README's `09-custom-look.png`, unchanged since 0.4.0.
- `09` and `10` are the App tests' `38-move-from-vortex` and `39-new-downloads` (`dotnet test
  tests/DAORework.App.Tests`, with `DAOREWORK_SCREENSHOT_DIR` set to a folder).
- `00-cover.png` is [cover.html](cover.html) at 1920x1080 (headless Edge: `msedge --headless --window-size=1920,1080
  --screenshot=<file> <cover.html>`). It is still the one built from the 0.4.0 `01-home.png` and `02-mods.png`, since
  its Home reads *Ready to play*; rebuilt from today's pictures it would read *Not checked yet*. Rebuild it when a
  Home picture shows the game ready.

They show the gilded-tome style (P2.5-28). If the window's look changes again, retake them as above.

## On upload day

Also choose: the category (Utilities), the generative-AI tag by Nexus's rules that day, and the file: the same zip as
the GitHub release (the release workflow uploads it by itself once set up, [nexus-upload.md](../nexus-upload.md)).
[nexus-page-draft.md](../nexus-page-draft.md) keeps the notes behind these choices.
