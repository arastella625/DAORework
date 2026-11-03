# Reviewing a pull request

How a PR is reviewed before it is marked ready, and what happens when a review comes in. The decision behind it is
[ADR-0029](adr/0029-pull-request-review.md). The rules a review checks against are in
[ENGINEERING_STANDARDS.md](ENGINEERING_STANDARDS.md) and the [SDD](SDD.md); this page is only the process.

## Before the PR is ready

Run these on the task branch, fix what they find, then open the PR (or take it out of draft).

| Pass | How | Looks for |
|---|---|---|
| 1. Gates | `dotnet format --verify-no-changes`, `dotnet build`, `dotnet test`, `scripts/check-file-size.ps1` | What CI will fail on. |
| 2. Correctness | `/code-review` in Claude Code | Bugs, wrong results, missing tests. |
| 3. Less code | `/ponytail-review branch` | Code that can be deleted, reused or made plainer. |
| 4. Security | `/security-review`, only when the diff touches `DAORework.Infrastructure`, `scripts/` or `.github/` | Paths, processes, downloads, secrets. |
| 5. Read it | The diff, top to bottom, against the PR checklist | Anything the tools missed. |

Put one line per pass in the PR description under *Review*: what it found and what was done about it.

### Ponytail findings

Ponytail argues for fewer abstractions. Our rules sometimes ask for more: new behavior goes in a **new** use case,
rule or strategy class, and a class takes at most five dependencies. When a finding contradicts the SDD,
ENGINEERING_STANDARDS, an ADR or LEGAL_BOUNDARIES, the rule wins: note the finding and the rule, and leave the code.
Everything else (dead code, a helper that duplicates one we have, a needless interface with one caller in the same
layer, an over-general parameter) is fixed in the PR if it is in the PR's own diff.

`/ponytail-audit` reviews the whole repository. Its findings become issues labelled `type: chore`, never changes
inside an unrelated PR.

## When a review comes in

A review comment can come from the owner, Copilot, SonarCloud or a Claude session. Each one gets an outcome:

1. **Fix it** in a new commit on the same branch, run pass 1 again, and reply with the commit.
2. **Or say why not** in a reply: the rule it would break, or why the code is right as it is.
3. **Out of scope** (about code the PR did not touch): open an issue and reply with its link.
4. Resolve the thread only after one of the above.

Rules that do not bend:

- No force-push that rewrites commits a reviewer has already seen; add commits instead.
- No skipping, disabling or weakening a test or a gate to get green.
- A PR is not merged with an unanswered thread or a red check.
- The owner merges. A tool or a session never approves or merges.

## Setting up Ponytail

Each contributor installs it in their own Claude Code; the repository does not enable it for you. Run these as two
separate prompts, then start a new session:

```
/plugin marketplace add DietrichGebert/ponytail
/plugin install ponytail@ponytail
```

Install it only from `DietrichGebert/ponytail`. Set it to `lite` with `{ "defaultMode": "lite" }` in
`~/.config/ponytail/config.json`; `/ponytail full` or `/ponytail off` changes it for one session. Without it, skip
pass 3 and say so under *Review*.
