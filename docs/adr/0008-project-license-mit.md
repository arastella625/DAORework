# ADR-0008: License the project under MIT

- **Status:** Superseded by [ADR-0020](0020-license-noncommercial.md) (2026-10-31: no selling)
- **Date:** 2026-09-29

## Context
The repository had no license file. Without one, nobody can legally contribute
to, reuse or redistribute the code, which defeats the goal of a community
project. The choice also decides which outside code the project may absorb
(ADR-0009): a permissive license cannot take in GPL code.

## Decision
DAORework is licensed under the **MIT License** (`LICENSE` at the repository
root). Contributions are accepted under the same license, which CONTRIBUTING.md
states. Ported third-party code is recorded in `THIRD-PARTY-NOTICES.md`.

## Consequences
The project matches the license of its dependencies and of the reusable
community tooling (DAOTools). It can be embedded in other launchers or mod
managers. It cannot copy code from GPL projects such as xoreos; it may study
their documentation and behavior.

## Alternatives considered
- *GPL-3:* would permit reusing xoreos code, but would stop others from
  embedding DAORework, and the code we would want from xoreos is engine code,
  which the roadmap does not build before Phase 4.
- *Apache-2:* fine, but adds a patent clause and notice requirements with no
  practical benefit for this project.
