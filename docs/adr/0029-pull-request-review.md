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
- Ponytail is installed by each contributor in their own Claude Code, from `DietrichGebert/ponytail` only, and run at
  `lite` (`PONYTAIL_DEFAULT_MODE=lite` or `{ "defaultMode": "lite" }` in `~/.config/ponytail/config.json`), so it
  trims code without arguing with the SDD while writing. The repository does not enable it for every session.
- **Precedence:** the SDD, ENGINEERING_STANDARDS, the ADRs and LEGAL_BOUNDARIES win over any review tool. A finding
  that contradicts one of them is answered by naming the rule and is not applied.
- Every review comment on a PR gets an outcome: fixed in a new commit, or a reply saying why not. A thread is resolved
  only after that. A PR is not merged with an unanswered thread.
- Review fixes stay inside the PR's task. A finding about code the PR did not touch becomes an issue, not a change
  (refactor and feature PRs stay separate).
- The owner merges. No tool or session approves or merges a PR.

## Consequences
- Each PR costs a few more minutes of agent time before it is ready, and arrives with fewer obvious issues.
- A session without Ponytail installed skips pass 3 and says so in the PR; the other passes still apply.
- Ponytail is third-party code that runs hooks in the sessions that install it. Keeping it out of the repository's
  `.claude/settings.json` means nobody runs it without choosing to.
- Running these passes in GitHub Actions on every PR (for example with `anthropics/claude-code-action`) needs an
  Anthropic API key as a repository secret; that is a separate, later decision.

## Alternatives considered
- *Keep self-review only:* the writer reviewing its own diff misses what a different lens finds.
- *Enable Ponytail for every session in `.claude/settings.json`:* third-party hooks would run for anyone who opens
  the folder, without their choice.
- *Ponytail at `full` or `ultra` while writing:* it would argue with the one-class-per-behavior rule on every task.
- *A CI review bot now:* needs a paid key and a secret; the local passes come first and show whether it is worth it.
