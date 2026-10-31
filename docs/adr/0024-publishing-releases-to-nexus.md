# ADR-0024: Publish each release to Nexus Mods from the release workflow

- **Status:** Accepted
- **Date:** 2026-10-31

## Context
DAORework 0.3.0 is on GitHub and on Nexus Mods ([mods/7083](https://www.nexusmods.com/dragonage/mods/7083)). The
release workflow publishes the GitHub Release; the Nexus file was uploaded by hand. Doing that by hand for every
release risks a Nexus zip that is not the GitHub one, whose checksum then matches nothing on the release page or the
website. Nexus Mods publishes an official GitHub Action, `Nexus-Mods/upload-action`, which adds a new version to an
existing file through their v3 API.

## Decision
- After the GitHub Release is published, the release job uploads **the same zip** to Nexus as a new version of the
  mod's main file, archives the previous version, sets the mod's version, and, when the mod id is set, adds a changelog
  entry that links the GitHub release notes.
- **Optional, like signing:** with the `NEXUS_API_KEY` secret and the `NEXUS_FILE_ID` variable in the `release`
  environment, it uploads; with neither, it is skipped with a notice; with only one, the run stops before anything is
  published. `NEXUS_MOD_ID` is optional and only adds the changelog entry.
- The action is **pinned to a commit** (v1.0.0-beta.11): it is third-party code that receives the API key, so a moved
  tag must not change what runs. Updating it is a reviewed change to the workflow.
- This runs in GitHub's build, not in DAORework: the app still never goes online (SDD §12).

## Consequences
- A tag updates GitHub and Nexus with one zip and one checksum.
- The Nexus API key is a personal key with access to the owner's Nexus account. It lives only as a secret in the
  `release` environment, which only runs for `v*` tags; GitHub masks it in logs. If it leaks, the owner revokes it on
  Nexus and creates a new one.
- The action is a beta and Nexus calls its v3 API an evaluation; if an upload fails, the GitHub Release is already
  out, and the owner uploads the same zip by hand.

## Alternatives considered
- *Keep uploading by hand:* easy to upload a different zip, and one more step after every release.
- *Call the Nexus API from a script of our own:* more code to keep up with an API that is still changing; the official
  action is maintained by Nexus.
