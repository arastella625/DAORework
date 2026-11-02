# ADR-0027: Run SonarCloud analysis from the CI workflow

- **Status:** Accepted
- **Date:** 2026-11-02

## Context
CI already enforces formatting, warnings-as-errors, tests, a coverage floor and the file-size limit. None of these
report code smells, duplication, security hotspots or the trend of new code over time. SonarCloud does, and the
`arastella625_DAORework` project exists. SonarCloud offers two analysis modes: *Automatic Analysis*, which reads the
repository without building and has no coverage, and *CI-based analysis*, which wraps our own build and test run. The
C/C++ Build Wrapper shown in the SonarCloud setup page does not apply to a .NET solution; the .NET route is
`dotnet-sonarscanner`.

## Decision
- The `build` job in `ci.yml` wraps the existing build and test steps with `dotnet sonarscanner begin` and `end`.
  It adds no second build and no second test run.
- Coverage is sent as **OpenCover**, because the .NET scanner does not read Cobertura. The test step asks coverlet
  for `cobertura,opencover`; `scripts/check-coverage.ps1` keeps reading Cobertura and is unchanged.
- **Optional, like signing:** the Sonar steps run only when the `SONAR_TOKEN` repository secret is available. Fork
  PRs get no secrets, so the steps are skipped there and the build still passes.
- SonarCloud's *Automatic Analysis* must be off for this project; it conflicts with CI-based analysis.
- The first rollout is **report-only**: the SonarCloud quality gate does not fail the workflow
  (`sonar.qualitygate.wait` is not set). Making it blocking is a later, separate decision.
- Checkout uses `fetch-depth: 0`, which Sonar needs for blame and new-code detection.
- The `dotnet-sonarscanner` global tool is installed unpinned in this first version; pinning it is a follow-up.

## Consequences
- Every PR and every push to `main` gets a SonarCloud analysis with coverage, without a second build.
- The full-history checkout makes the job a little slower.
- `SONAR_TOKEN` is a project-analysis token; GitHub masks it in logs. If it leaks, revoke it in SonarCloud and
  replace the secret.
- SonarCloud's rules overlap partly with our analyzers and the hard limits in `ENGINEERING_STANDARDS`. Those remain
  the authority; a Sonar finding is advice until the gate is made blocking.
- This runs in GitHub's build, not in DAORework: the app still never goes online (SDD §12).

## Alternatives considered
- *Automatic Analysis:* no build, so no coverage and weaker C# results.
- *The C/C++ Build Wrapper from the setup page:* for C/C++ projects, not .NET.
- *Fail CI on the quality gate from day one:* the gate has never run on this code; a blocking gate on an unknown
  baseline would stop unrelated PRs.
- *A separate Sonar workflow:* would build and test the solution a second time.
