# ADR-0030: Keep the Nexus page's text in the repository, and send it to Nexus from a workflow

- **Status:** Accepted
- **Date:** 2026-11-04

## Context
The Nexus page's summary and description live in the repository already (`docs/release/nexus/summary.txt` and
`description.bbcode`), and their pictures load from the project website ([ADR-0021](0021-project-website.md)). The
owner pastes the text into Nexus by hand after each change, so the live page lags behind the files or drifts from
them. Nexus's v3 API has *Edit mod details* (`PATCH /v3/mods/{id}`), which replaces a mod's summary and its BBCode
description exactly as the page editor saves them. The caller must be on the mod's team. Nexus marks it experimental.

[ADR-0024](0024-publishing-releases-to-nexus.md) puts the Nexus API key in the `release` environment, which runs only
for `v*` tags. Page text changes on `main`, between releases.

## Decision
- **The files are the page.** `docs/release/nexus/summary.txt` and `description.bbcode` are the only place the page's
  text is changed. A change is a PR, like any other.
- **A workflow sends them.** `.github/workflows/nexus-page.yml` runs on a push to `main` that changes either file, and
  by hand (*Run workflow*). It sends both files with `PATCH /v3/mods/{NEXUS_MOD_ID}` and fails if Nexus refuses them.
  It never reads the PR's code: it runs on `main` only.
- **Its own environment.** The job runs in a `nexus-page` environment whose deployment branches are limited to `main`,
  with its own `NEXUS_API_KEY` secret and `NEXUS_MOD_ID` variable (the Unique Mod ID). With both set it sends; with
  neither it skips with a notice; with only one it fails, as ADR-0024's upload does.
- **Plain HTTP, no third-party action.** One `curl` call with the JSON built by `jq`, both already on GitHub's runners,
  so nothing extra receives the key.
- **Manual still:** the mod's name, the image gallery, permissions and the category. Nexus's API does not change them.

## Consequences
- Merging a change to the page's text updates Nexus within minutes; the owner no longer pastes it.
- The first run replaces whatever the live page says. Edits made on the Nexus website are overwritten by the next run,
  so they belong in the files instead.
- A second copy of the personal API key exists, in `nexus-page`. Revoking it on Nexus stops both environments; a new
  key goes into both secrets.
- The call is experimental. If Nexus changes or removes it, the workflow fails visibly and the owner pastes by hand
  again until it is fixed.
- A mod under moderation review can be edited by its team only when Nexus staff allow it; a run then fails, and is run
  again by hand once the review is over.

## Alternatives considered
- *Keep pasting by hand:* the page drifts from the repository, and every wording fix costs the owner a manual step.
- *Reuse the `release` environment:* it would have to allow `main`, so every job in it, signing included, could then
  run from `main` and not only from a tag.
- *Send the text from the release job:* page fixes would wait for the next release.
