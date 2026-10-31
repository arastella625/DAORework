# Contributing and workflow

This is the working agreement for DAORework. It applies with one contributor or
ten, so that the history stays clean and anyone can pick up a task cold. The
technical rules live in [docs/ENGINEERING_STANDARDS.md](docs/ENGINEERING_STANDARDS.md);
this page covers *process*.

## The flow

```
Issue (Backlog) → Ready → branch → PR → review + CI → squash-merge → Done
```

1. **Everything starts as an issue.** No branch without an issue. Use the templates
   (*User story*, *Task*, *Bug report*). Blank issues are disabled.
2. **Pick from Ready**, assign yourself, move the card to *In progress*.
3. **Branch** from `main`, named `<type>/<ID>-<short-slug>`:
   `refactor/P0-08-diagnostic-rules`, `feat/P1-05-save-backup`, `fix/42-profile-name-roundtrip`,
   `docs/adr-0008-mod-storage`, `chore/P0-04-ci`.
4. **Commit** with Conventional Commits and the task ID: `refactor(diagnostics): split rules (P0-08)`.
5. **Open a PR** that closes the issue (`Closes #42`), fill in the PR checklist, keep it
   under about 400 changed lines of production code. One task, one PR.
6. **Review + CI.** A PR merges only when CI is green and the checklist is satisfied.
   CI runs `dotnet format --verify-no-changes`, a warnings-as-errors build, the tests with
   coverage, `scripts/check-coverage.ps1` and `scripts/check-file-size.ps1`. Run the same
   commands locally before pushing (PowerShell 7 is needed for the two scripts).
   With one maintainer, self-review still means reading the diff top to bottom.
7. **Squash-merge.** The squash message keeps the Conventional Commit title.
   The issue closes and its card moves to *Done* automatically.

## Getting a working environment

Install the .NET SDK that `global.json` pins and PowerShell 7, the same versions CI
uses; then `dotnet build`, `dotnet test` and the two gate scripts work as they do in CI.
Work on Windows itself: `scripts/verify-real-install.ps1` needs the game install and
your Documents folder.

Dependency updates arrive weekly as Dependabot PRs (`.github/dependabot.yml`), grouped
by package family and labelled `type: chore`, `area: ci`. They merge like any other PR:
CI green, then squash.

## Verify before the PR

A task is shown working before its pull request is opened, so the review starts
from evidence rather than from a diff:

1. **Transcript or screenshots.** For CLI changes, the exact commands run and
   their output. For GUI changes, headless screenshots of every changed page.
   Both go in the PR description.
2. **Owner check against a real install.** Anything this project cannot verify
   without the game (detection, launching, real mod folders) gets a short
   checklist for the owner to run on their machine on the task branch.
3. **Then the PR.** It is opened only after the owner has seen the evidence and
   either confirmed it or asked for changes, which are made on the same branch first.

Docs-only changes skip step 1 and 2.

## Issue hierarchy

| Level | Label | What it is |
|---|---|---|
| Milestone | — | One per roadmap **phase**. Carries the phase exit gate in its description. |
| User story | `type: user-story` | A user-facing outcome. Groups its tasks as **sub-issues**. |
| Task | `type: task` | One roadmap task ID, one PR. |
| Bug | `bug` | A behavior that contradicts the docs or CLI help. |
| Admin / chore | `admin` or `type: chore` | Process, tooling, decisions. Not attached to a story. |

GitHub issue *types* are an organization feature and are not available on this
personal repository, so the `type:` labels carry that role.

Every product issue belongs to a milestone. Admin issues may not.

## Labels

- `type: user-story`, `type: task`, `type: chore`, `type: docs`, `bug`
- `area: core`, `area: application`, `area: infrastructure`, `area: cli`, `area: ci`, `area: docs`
- `admin` for process and project management work
- `needs-decision` when an issue waits on an SDD open decision; `blocked` when it waits on another issue
- `good first issue` and `help wanted` for newcomers

Priority and status are **not** labels: they are fields on the project board.

## The project board

The board is a GitHub Project (the new Projects) named **DAORework** attached to
this repository. Columns are the *Status* field:

| Status | Meaning |
|---|---|
| Backlog | Filed, not yet groomed. Default for new issues. |
| Ready | Groomed: clear scope, DoD, milestone, labels. Can be picked up now. |
| In progress | Someone is assigned and a branch exists. |
| In review | A PR is open. |
| Done | Merged or closed. |

Extra fields: **Priority** (P0 blocker, P1 high, P2 normal, P3 low) and **Size** (XS to XL).
Milestones supply the phase, so the board needs no phase field.

### Re-creating the board

If the board is ever lost, or a fork needs one:

1. Repository → *Projects* → *New project* → *Board* template → name it `DAORework`.
2. *Settings* → *Fields*: edit *Status* to the five values above; add *Priority*
   (single select: P0, P1, P2, P3) and *Size* (single select: XS, S, M, L, XL).
3. *Settings* → *Workflows*: enable **Auto-add to project** for this repository
   (filter `is:issue,pr is:open`), **Item closed → Done**, **Pull request merged → Done**,
   and **Item added → Backlog**.
4. Add a *Table* view grouped by *Milestone* for phase planning, and a *Board*
   view filtered to `label:admin` for administration.

## Releasing

1. In one PR: bump `<Version>` in `Directory.Build.props`, and move *Unreleased* in `CHANGELOG.md` under a new
   `## [x.y.z] - date` heading. The Release workflow builds the Windows zip on that PR; download it from the run's
   artifacts and try it before merging.
2. Merge, then tag the merge commit and push the tag:
   `git tag -a vx.y.z <sha> -m "vx.y.z" && git push origin vx.y.z`.
3. The Release workflow rebuilds, smoke-tests, signs if signing is set up ([docs/release/code-signing.md](docs/release/code-signing.md)),
   attests the build and publishes the GitHub Release, with the changelog section as its notes. `0.x` versions are
   marked as pre-releases.
4. Submit the release zip to Microsoft's file analysis as its developer
   (<https://www.microsoft.com/wdsi/filesubmission>, *Software developer*, "incorrectly detected" if Defender or
   SmartScreen flagged it). The release job uploads the same zip to Nexus when that is set up
   ([docs/release/nexus-upload.md](docs/release/nexus-upload.md)); otherwise upload it by hand.
5. Put the zip's SHA-256 (from its `.sha256` file) and the version in the *Check your download* section of
   `site/public/index.html` and merge it, so the website ([ADR-0021](docs/adr/0021-project-website.md)) shows the
   checksum from a second host.

To build the zip locally: `./scripts/publish-release.ps1` (output in `./artifacts`).

## Decisions

A design choice not covered by the SDD gets an ADR (`docs/adr/0000-template.md`)
before the code merges. Open SDD decisions (D-1, D-4) are tracked as `admin` +
`needs-decision` issues so they show up on the board instead of being forgotten.

## License and third-party code

By contributing you agree that your contribution is licensed under the project's
[PolyForm Noncommercial License 1.0.0](LICENSE). Ported code must be permissively licensed and listed in
`THIRD-PARTY-NOTICES.md`; GPL code is never copied. See
[ADR-0020](docs/adr/0020-license-noncommercial.md) and
[ADR-0009](docs/adr/0009-third-party-mods-and-tools.md).

## Definition of Done

See [ENGINEERING_STANDARDS.md §8](docs/ENGINEERING_STANDARDS.md#8-definition-of-done).
The Task template pre-fills it as a checklist.
