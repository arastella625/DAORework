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

1. **Scan it yourself first.** Upload the zip from the GitHub release to [VirusTotal](https://www.virustotal.com) and
   keep the link to its report. It names the engines that flag the zip, and their detection names, without waiting for
   Nexus to say. The scan is done by hand: a scan in CI waits on P2.6-07's results.
2. **Ask for the review.** The release run's summary (Actions → Release → the tag's run) has the email to
   `support@nexusmods.com` filled in with the version, the zip's SHA-256, the release link, the attestation command
   and how to build it, from [nexus-review-request.md](nexus-review-request.md). Copy it, add the file id from the
   quarantine notice and the VirusTotal link, and send it. It asks which antivirus engines flagged the zip, and their
   detection names.
3. **Report each false positive.** For every engine VirusTotal or Nexus names, send the zip to that vendor as a false
   positive, with the detection name and the link to the source. Start with Microsoft Defender, which most Windows players run:
   [submit a file to Microsoft](https://www.microsoft.com/en-us/wdsi/filesubmission), as a software developer, and
   choose *incorrectly detected*. For other engines, use the vendor's own false-positive page. Each cleared report
   makes the next release less likely to be flagged.
4. **If support doesn't answer in a week** (P2.6-80), go on in this order, and stop at the first that works. Nexus's
   help page names email and a moderator on the website as the two ways to ask. Never delete the held file: Nexus
   says that breaks the page's version history.
   1. Reply on the same email, rather than starting a new one, saying when you first wrote.
   2. Send a private message on the site to a moderator: one for Dragon Age: Origins, or a Community Manager (staff
      have a *Staff* badge on the forums). Give the mod page, the file id, when it was held and when you emailed, and
      the source, release and VirusTotal links.
   3. Post the same in the forums' *Site Support* section, where staff answer in public.
5. **Note the outcome** on [#238](https://github.com/arastella625/DAORework/issues/238): the version, when it
   was held and cleared, which engines flagged it, and which way of asking worked. That shows whether P2.6-07's
   release changes and the steps above help.
