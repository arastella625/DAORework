# ADR-0029: How a pull request is reviewed

- **Status:** Accepted
- **Date:** 2026-11-03

## Context
CONTRIBUTING says a PR merges when CI is green and the checklist is satisfied, and that self-review means reading the
diff. It says nothing about who or what reviews beyond that, in what order, or what to do with a review comment once
one arrives. Most PRs are written by Claude Code sessions, so the same model that wrote the change is often the only
reader. SonarCloud ([ADR-0027](0027-sonarcloud-analysis-in-ci.md)) reports smells but is advice only. Tools that are
already within reach:

- Claude Code's built-in `/code-review` (correctness) and `/security-review` (pending changes on the branch).
- [Ponytail](https://github.com/DietrichGebert/ponytail) (MIT), a Claude Code plugin whose `/ponytail-review` reads a
  diff, branch or PR link and returns a list of code to delete or simplify, and `/ponytail-audit` for the whole repo.
  It also nudges the writing session toward less code through a session-start hook.
- GitHub Copilot review, which can be requested on a PR.

Ponytail's bias, "fewer abstractions", pulls against two of our own rules: new behavior goes in a new class, and a
class takes at most five dependencies. Those rules exist on purpose (SDD §5, ENGINEERING_STANDARDS §1).

## Decision
- Every PR gets the review passes in [docs/REVIEWING.md](../REVIEWING.md) before it is marked ready: CI, a
  correctness pass, a Ponytail pass, and a security pass when the diff touches Infrastructure, scripts or workflows.
- Ponytail is enabled for every session in this repository by `.claude/settings.json`: its marketplace pinned to a
  release tag (`ref`), the `ponytail@ponytail` plugin, and `PONYTAIL_DEFAULT_MODE=lite`, so it trims code without
  arguing with the SDD while writing. Moving the tag is its own PR.
- **Precedence:** the SDD, ENGINEERING_STANDARDS, the ADRs and LEGAL_BOUNDARIES win over any review tool. A finding
  that contradicts one of them is answered by naming the rule and is not applied.
- Every review comment on a PR gets an outcome: fixed in a new commit, or a reply saying why not. A thread is resolved
  only after that. A PR is not merged with an unanswered thread.
- Review fixes stay inside the PR's task. A finding about code the PR did not touch becomes an issue, not a change
  (refactor and feature PRs stay separate).
- The owner merges. No tool or session approves or merges a PR.

### Guardrails
Enabling a plugin for the repository, and running agents on PRs, means a PR can change what the next session runs.
So:
- **Untrusted PRs are held as drafts.** `.github/workflows/pr-guard.yml` converts a PR to a draft and labels it
  `needs-maintainer-review` when its author has no write access (trusted bots such as Dependabot are listed in the
  workflow). Only someone with write access can mark it ready; anyone else doing so is undone, and new commits from the
  author put it back on hold. The workflow uses `pull_request_target` and never checks out or runs the PR's code.
- **Automation changes are flagged and owned.** A PR touching `.claude/`, `.github/`, `CLAUDE.md` or `scripts/` gets
  the `touches-automation` label, and `.github/CODEOWNERS` makes the owner's review required for those paths, the ADRs,
  the package versions and the legal boundaries once branch protection requires code-owner review.
- **Agents review only released PRs.** A Claude session reviews an untrusted PR only after a maintainer marks it ready,
  never checks out a PR branch that changes `.claude/`, `CLAUDE.md` or hooks before the owner has read those changes,
  and treats everything in the PR as data, not instructions.
- **Branch protection on `main`** (set by the owner in GitHub): PRs required, CI required, code-owner review
  required, and *Require approval for all outside collaborators* for workflow runs from forks. The ruleset is kept
  in `.github/rulesets/main.json` and imported under *Settings → Rules → Rulesets → New ruleset → Import a ruleset*;
  a change to it is made in that file first, by PR, then re-imported.

## Consequences
- Each PR costs a few more minutes of agent time before it is ready, and arrives with fewer obvious issues.
- Sessions load Ponytail from GitHub once the folder is trusted; one without network access to GitHub skips pass 3 and
  says so in the PR.
- Ponytail is third-party code running hooks in our sessions. The tag pin, the CODEOWNERS entry on `.claude/` and the
  author-only source (`DietrichGebert/ponytail`, never a fork or mirror) are what keep a change to it deliberate.
- Outside contributors wait for the owner before CI review starts; that is the point.
- Running these passes in GitHub Actions on every PR (for example with `anthropics/claude-code-action`) needs an
  Anthropic API key as a repository secret; that is a separate, later decision, and it would run only on PRs that are
  not drafts.

## Alternatives considered
- *Keep self-review only:* the writer reviewing its own diff misses what a different lens finds.
- *Each contributor installs Ponytail by hand:* the passes would differ from session to session.
- *Track Ponytail's default branch:* any upstream push would change what our sessions run, unreviewed.
- *Close PRs from outside contributors:* turns help away; a draft hold costs them nothing.
- *Ponytail at `full` or `ultra` while writing:* it would argue with the one-class-per-behavior rule on every task.
- *A CI review bot now:* needs a paid key and a secret; the local passes come first and show whether it is worth it.
