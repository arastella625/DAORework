# Uploading releases to Nexus automatically: setup for the owner

After this one-time setup, pushing a `v*` tag also puts the release zip on the Nexus page as a new version of the
main file ([ADR-0024](../adr/0024-publishing-releases-to-nexus.md)). Without it, releases still publish to GitHub, and
the zip is uploaded to Nexus by hand.

## 1. On Nexus Mods

1. **The file id:** on the mod page ([mods/7083](https://www.nexusmods.com/dragonage/mods/7083)), open the **Files**
   tab, tick **Advanced**, and note the number shown for the main file (DAORework 0.3.0). The mod id is `7083`.
2. **An API key:** open [your API keys](https://www.nexusmods.com/settings/api-keys) and copy your personal API key.
   Treat it like a password: it acts as your Nexus account.

## 2. On GitHub

In the repository: **Settings** → **Environments** → `release` (create it if it does not exist yet):

| Kind | Name | Value |
|---|---|---|
| Secret | `NEXUS_API_KEY` | the API key from step 1 |
| Variable | `NEXUS_FILE_ID` | the main file's id |
| Variable | `NEXUS_MOD_ID` | `7083` (optional: adds a changelog entry linking the release notes) |

The key goes in **Environment secrets**, never in a variable, a file or a comment.

## 3. What happens on each tag

The release job checks the setup first: both set, it uploads; neither set, it skips with a notice; only one set, it
stops before anything is published. After the GitHub Release is out, the same zip is uploaded as "DAORework
<version>", the previous version is archived, and the mod's version is updated.

If the upload step fails, the GitHub Release is already out: upload that same zip on Nexus by hand, and say what
failed so the workflow can be fixed. To stop the automatic upload, delete the secret and the variables.

If the key ever leaks, revoke it on the Nexus API keys page and put a new one in the secret.
