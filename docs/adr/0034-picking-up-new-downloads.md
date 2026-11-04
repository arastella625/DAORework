# ADR-0034: Picking up new downloads from the player's own browser

- **Status:** Proposed. Becomes Accepted when the owner schedules P3-10.
- **Date:** 2026-11-03

## Context
Getting a mod into DAORework takes a trip through a file picker: download it, come back, press *Add mods*, find the
file. The owner asked on 2026-11-03 whether DAORework should run its own browser, limited to Dragon Age mods on Nexus,
so downloads could be dealt with automatically.

The wish is sound, but a browser is a heavy way to meet it:
- **It saves no clicks.** The player still chooses and downloads each mod. The
  [Nexus API policy](https://help.nexusmods.com/article/114-api-acceptable-use-policy) also bars using a player's key
  "without the action being initiated by the user", so downloading in bulk through the API is out either way.
- **It ends "never goes online".** SDD §12 allows no network access without an ADR and an opt-in, and the first access
  planned is one anonymous request to GitHub (P3-06). A browser would load the live web, ads and scripts included.
- **Players would type their Nexus password into our window**, and any browser bug would become our security problem.
  [ADR-0018](0018-mod-safety.md) makes safety part of why a player trusts DAORework.
- **Nexus's view is unknown.** Its API policy does not mention embedded browsers.

The player's own browser already does this job. What is missing is the step after it: noticing the file. That needs no
network access at all. Some facts from the code shape how:
- `ArchiveClassifier` calls any archive without a `.dazip` an override mod, so it cannot tell a mod from another zip.
- `ModSource` carries a SHA-256, so opening a source to import it reads the whole file. Listing must not.
- Importing already does everything needed once the file is chosen: the scan, programs flagged, and the known downloads.
- `IKnownFolders` has no Downloads folder, and .NET's `SpecialFolder` does not offer one.

## Decision

### 1. Off until the player says yes
File names in a Downloads folder are private, and the archives there are not all mods, so looking is opt-in. Home asks
once (*Add mods straight from your downloads?*, with *Turn on* and *No thanks*), and *New downloads* in Settings turns
it on or off. Only files that arrive after it is turned on are offered. Older ones stay a job for *Add mods*.

### 2. Where it looks
- **The folder Windows reports:** `IKnownFolders` gains `Downloads`, from the shell's known folder
  (`SHGetKnownFolderPath`), so a Downloads folder moved to another drive is found where it is. Nothing assumes `C:`.
- **The player can name another**, with *Choose folder*, for a browser that saves somewhere else. It is one folder, not
  a list.
- **Its top level only,** where browsers save. It does not go into subfolders or follow links.
- **A folder that is missing** is said once (`DAO_DOWNLOADS_FOLDER_MISSING`, a warning), with *Choose folder*.

### 3. When it looks
It looks when the window comes back into focus and when the Mods page opens, and *Look again* does it by hand. A player
coming back from their browser is the moment that matters. Nothing runs in the background, and nothing runs when
DAORework is closed. The look runs off the calling thread, and leaving the page cancels it.

### 4. What counts as a new mod
A file is offered when all of these hold:
- **It is a download:** `.dazip`, `.zip`, `.7z` or `.rar`, the same list as *Add mods* (moved to one place in Core).
- **It is finished:** it is not empty, it opens for reading, and no `<name>.part` sits beside it. Chromium's
  `.crdownload` never matches the list. A file that fails this is looked at again next time.
- **It is new:** the later of its created and changed times is after the moment the player turned this on, and it is
  not on the list of files already dealt with.
- **It looks like a Dragon Age mod:** a new rule in Core, `ModDownloadRule`, reads the archive's file names only. A
  `.dazip` always passes. An archive passes if it holds a `.dazip`, an add-in's `Manifest.xml` and `Contents/`, or at
  least one file of the game's own data (`.erf`, `.gda`, `.tlk`, `.ncs`, `.uti`, `.utc`, `.mmh`, `.msh`, `.mao`, or
  `chargenmorphcfg.xml`).

`.dds` does not count on its own, because every game on Nexus uses it. A retexture holding only `.dds` files is not
offered, and *Add mods* still takes it. A mod missed is better than offering a player's other games.

An archive that cannot be listed (damaged, passworded, or over `FolderModLibrary.MaxFiles`) is not offered. Only the
list of names is read: nothing is extracted, hashed or scanned until the player says so.

### 5. What the player sees
- **Home:** one suggestion under *Worth a look* (*2 new mods in your downloads*) that goes to the card.
- **Mods:** a card, *New downloads*, lists each file by name, leaving off the Nexus id and version as
  [ADR-0028](0028-taking-over-from-vortex.md) §2 does. Each has *Add* and *Not this one*, and the card has *Add all*.
  It adds no tab ([ADR-0026](0026-workflow-first-layout.md) §2).
- **Adding is the existing import.** *Add* calls `ImportMod` as *Add mods* does, with the same checks and messages.
- **A file dealt with is not offered again,** whether added, refused by the antivirus, or set aside with *Not this one*.
  It is known by its path, size and changed time, so no file is read twice.

### 6. What never happens
- **A file is never added without a click.** Adding never changes the game, but it extracts an archive the player has
  not looked at, and a site can start a download by itself.
- **A download is never moved, renamed or deleted.** The folder is only ever read.
- **Nothing goes online.** SDD §12 is unchanged, and P3-06 stays the first network access planned.
- **File names stay private.** Logs give counts, never names. The support bundle leaves out the folder's contents and
  the list of files dealt with.

### 7. Opening a mod's page
Where the window names a known item's `source`, such as the warning that a download differs from the known ones, it
offers *Open the mod's page*. That opens the page in the player's default browser through `IUriLauncher`, and the file
is then picked up as above. Only `https` links open. A test holds every catalog `source` to that.

### 8. The command line
`mods downloads` lists what would be offered, and takes `--folder <path>`. Asking is consent, so it works with the
window's setting off. It only reads, and it prints paths, which the existing `mods import <path>` takes.

### 9. Where the code goes
- **Core:** `ModDownloadRule`, the extension lists, and the rule for what is new. All pure, with tests.
- **Application:** a new `FindNewDownloads` use case and a `SetDownloadHandled` one, with two ports: `IDownloadsFolder`
  (finished files, and an archive's file names) and `INewDownloadsStore`.
- **Infrastructure:** both adapters, and `Downloads` in the known folders, with a P/Invoke as in ADR-0018 and no new
  package. Listing uses `ZipArchive` and SharpCompress directly, not `ModSource`, so no file is hashed.
- **Persisted:** `new-downloads.json` in the app-data folder, with a schema version (SDD §8.4): on or off, the folder,
  when it was turned on, and the files dealt with. Entries for files that are gone are dropped. It is its own file so
  that `preferences.json` stays free of file names.
- **Words:** VOICE.md gains *New downloads*.

### 10. What this leaves for later
Nexus's *Mod Manager Download* button (`nxm://`) stays out, and needs its own ADR. It would add a real gain: the link
names the exact mod and file. It needs network access with an opt-in (SDD §12), a registered app rather than personal
keys, a way to share the link type with Vortex, and Nexus's rules for free and premium accounts, which its API policy
does not cover. P3-12 asks Nexus first. Whatever brings a file in later can hand it to the same *New downloads* card.

## Consequences
- A player downloads as they always have, comes back, and presses *Add*. No file picker, and no change to how they
  use Nexus.
- It is still not one click: the browser and *Add* are two steps. That is the price of staying offline.
- **Archive readers now run on files nobody chose.** Today one runs only on a file the player picked. The opt-in, names
  only and no extraction keep that small, but a crafted file can reach SharpCompress's header parsing, so P3-10 needs
  `/security-review`, and the package stays up to date.
- **Dragon Age II mods look the same.** They share `.dazip` and `.erf`, so one can be offered. The player sees its name
  and chooses, which is another reason for the click in §6.
- `.dds`-only mods are missed (§4), and a player may wonder why. The card's empty state says *Add mods* takes any mod.
- The catalog knows only two third-party mods with pages today, so §7 is small until it grows. A guided list of mods to
  get would need that growth first, and is not part of this ADR.
- `ShellUriLauncher`'s error tells the player to check Steam, which is wrong for a web page. P3-11 makes it general.
- Phase 2.7 needs a Linux answer for `Downloads` (the XDG user folder), behind the same port.

## Alternatives considered
- **A built-in browser limited to Nexus:** see Context. The limit also leaks, since sign-in, download servers, ads and
  scripts sit on other domains, and mod descriptions link elsewhere.
- **`nxm://` links now:** the real one-click route, but Vortex and Mod Organizer 2 already have it, and it brings
  network access, registration and shared link handling. §10 leaves the way open.
- **Add new files by themselves:** the fullest answer to the wish, and adding never changes the game. It would extract
  files nobody looked at, and fill the library with Dragon Age II mods. It can be a setting later, if *Add all* proves
  too much.
- **A `FileSystemWatcher`:** offers a file sooner, but the player is in their browser then, and it sees files mid-write.
  Looking on focus needs no background work and misses nothing that arrived while DAORework was closed.
- **Offer every archive:** no rule to keep up, but it would list every zip a player downloads.
- **Reuse `ArchiveClassifier`:** it answers what shape a mod is, and assumes the file is one.
- **Read the mod id from the Nexus file name:** a file name can say anything, so it cannot show which game a mod is for.
- **Move the file out of Downloads once added:** tidy, but it writes to a folder that is the player's.
