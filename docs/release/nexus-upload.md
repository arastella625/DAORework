# Uploading releases to Nexus automatically: setup for the owner

After this one-time setup, pushing a `v*` tag also puts the release zip on the Nexus page as a new version of the
main file ([ADR-0024](../adr/0024-publishing-releases-to-nexus.md)). Without it, releases still publish to GitHub, and
the zip is uploaded to Nexus by hand.

## 1. On Nexus Mods

1. **The file id:** on the mod page ([mods/7083](https://www.nexusmods.com/dragonage/mods/7083)), open the **Files**
   tab, tick **Advanced**, and note the number shown for the main file (DAORework 0.3.0). The mod id for the upload
   is the **Unique Mod ID** shown in the same place (`601295428523`), not the `7083` in the page's address.
2. **An API key:** open [your API keys](https://www.nexusmods.com/settings/api-keys) and copy your personal API key.
   Treat it like a password: it acts as your Nexus account.

## 2. On GitHub

In the repository: **Settings** → **Environments** → `release` (create it if it does not exist yet):

| Kind | Name | Value |
|---|---|---|
| Secret | `NEXUS_API_KEY` | the API key from step 1 |
| Variable | `NEXUS_FILE_ID` | the main file's id |
| Variable | `NEXUS_MOD_ID` | `601295428523`, the Unique Mod ID (optional: adds a changelog entry linking the release notes) |

The key goes in **Environment secrets**, never in a variable, a file or a comment; the release job stops if it finds
the key saved as a variable. The file id and mod id may be variables or secrets.

## 3. What happens on each tag

The release job checks the setup first: both set, it uploads; neither set, it skips with a notice; only one set, it
stops before anything is published. After the GitHub Release is out, the same zip is uploaded as "DAORework
<version>", the previous version is archived, and the mod's version is updated.

If the upload step fails, the GitHub Release is already out: upload that same zip on Nexus by hand, and say what
failed so the workflow can be fixed. To stop the automatic upload, delete the secret and the variables.

If the key ever leaks, revoke it on the Nexus API keys page and put a new one in the secret.

## 4. After each upload: Nexus's manual check

Nexus holds every upload that contains programs until its staff have checked it by hand, so each new version is
quarantined at first (0.3.1 and 0.4.0 both were). The README, the website and the Nexus description say so, and point
players to the same zip on GitHub in the meantime.

1. **Ask for the review.** Email `support@nexusmods.com` with the subject *Review request: DAORework <version>
   quarantined (Dragon Age: Origins, mod 7083)*. Give the file id from the quarantine notice, the GitHub release link
   and the zip's SHA-256 (from the release's `.sha256` file), and say how to check it: the build attestation
   (`gh attestation verify DAORework-<version>-win-x64.zip --repo arastella625/DAORework`), the code signature, and
   how to build it (`pwsh ./scripts/publish-release.ps1`, see CONTRIBUTING.md). Say it works offline, includes no game
   files, and does not touch Steam's DRM. Ask which antivirus engines flagged it, and their detection names.
2. **Report each false positive.** For every engine Nexus names, send the zip to that vendor as a false positive, with
   the detection name and the link to the source. Start with Microsoft Defender, which most Windows players run:
   [submit a file to Microsoft](https://www.microsoft.com/en-us/wdsi/filesubmission), as a software developer, and
   choose *incorrectly detected*. For other engines, use the vendor's own false-positive page. Each cleared report
   makes the next release less likely to be flagged.
3. **Note the outcome** on [#87](https://github.com/arastella625/DAORework/issues/87): which engines, how long the
   review took. That shows whether the release changes in P2.6-07 help.
