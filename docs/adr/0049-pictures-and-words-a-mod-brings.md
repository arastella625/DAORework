# ADR-0049: Pictures and words a mod brings

- **Status:** Accepted (by the owner, 2026-11-15)
- **Date:** 2026-11-15

## Context
On 2026-11-15 the owner asked for each mod to have a page a player can enjoy, with the preview of a hairstyle where
there is one (Phase 2.10). Nothing in DAORework shows a mod's pictures, description or author today:
- `mod.json` holds an id, a name, a kind, files, an add-in UID, the download's name and hash, and the antivirus verdict.
  No description, author or picture.
- `AddInManifest` reads a `.dazip`'s UID and title, and nothing else from its `Manifest.xml`.
- A download that is a zip holding a `.dazip`, the usual shape on Nexus Mods, is unwrapped and only the `.dazip` is
  kept. The readme and screenshots beside it are thrown away.
- An override mod keeps every file it came with, readmes and pictures included ([ADR-0036](0036-only-mod-files-reach-the-game.md)).

The rules that bound this: DAORework never goes online without an ADR and the player's yes (SDD §12), never ships a
mod's files or pictures ([LEGAL_BOUNDARIES](../LEGAL_BOUNDARIES.md), [ADR-0026](0026-workflow-first-layout.md) §5),
and treats every mod as untrusted ([ADR-0018](0018-mod-safety.md)). The owner chose on 2026-11-15 that pictures come
only from the mod's own download or from the player.

## Decision
1. **What counts as a picture:** a `.png`, `.jpg`, `.jpeg`, `.webp`, `.gif` or `.bmp` file the mod came with: next to
   a `.dazip` in its download, inside a `.dazip` but outside its `Contents` folder, or anywhere in an override mod.
   `.dds` and `.tga` are the game's textures and are never shown. Nothing about deploying changes: a picture in an
   override mod still goes into the game as it does today, so ADR-0036's list stays as it is.
2. **The download's extras are kept.** When a `.dazip` is unwrapped from a zip, the zip's other files (readmes and
   pictures) are kept in the mod's Library folder under `download/`. They are never deployed and never compared.
3. **Words:** the description is the `Manifest.xml` description for an add-in, else the first paragraph of the mod's
   first `.txt` or `.md` readme, cut to about 400 characters. The author is the `Manifest.xml` publisher, else none.
   Both are plain text: no markup, links or pictures in them are rendered.
4. **The player's own picture:** *Add a picture* copies one picture into the mod's Library folder under `pictures/`,
   decoded and scaled as a look's picture is ([ADR-0017](0017-custom-looks.md)). The player picks which picture leads.
5. **Untrusted pictures:** every picture is decoded in Infrastructure with SkiaSharp under the limits
   `SkiaBackgroundImageImporter` uses (50 MB, 100 million pixels), and only a small thumbnail is kept, in a cache that
   can be deleted and rebuilt. A picture that won't decode is skipped without a word on the row and named under
   *Details*. Pictures and descriptions never go into a support bundle or a log.
6. **Stored:** `mod.json` goes to schema 4, with optional `Description`, `Author` and `Pictures` (Library-relative
   paths, the lead one first). Schema 3 is read as before. A mod already in the Library is filled in from the files it
   kept; extras already thrown away can't come back, and the page offers *Add a picture*.
7. **Still offline.** Nothing is fetched from Nexus Mods or anywhere else. *Open the mod's page* still opens the
   player's own browser.

## Consequences
- A mod with screenshots in its download shows them, and its page has words without anyone typing them.
- Many mods have neither. Their page shows a crest in the theme's colours ([ADR-0050](0050-a-calmer-window.md)), and
  *Add a picture*.
- The Library grows by a download's readme and pictures, usually a few hundred KB a mod.
- Import and the Library's migration change, so P2.10-04 needs fault-injection cases for unwrapping with extras.

## Alternatives considered
- *Pictures and descriptions from Nexus Mods:* needs network access, each player's own API key, and Nexus's terms for
  automated use. The owner chose offline on 2026-11-15.
- *Pictures in the catalog:* the catalog is shipped, so it would ship authors' pictures.
- *Showing `.dds` textures:* most are parts of a model, not a preview, and decoding them is a format reader of its own.
- *Holding pictures back from the game:* ADR-0036 refuses to guess what the game reads, and a picture deployed by
  mistake costs nothing.
