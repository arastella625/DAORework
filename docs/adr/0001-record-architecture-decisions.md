# ADR-0001: Record architecture decisions

- **Status:** Accepted
- **Date:** 2026-09-28

## Context
This is a long-lived, part-time project. Decisions made months apart get forgotten,
then re-argued or quietly contradicted.

## Decision
Every significant design choice gets an ADR in `docs/adr/`, numbered sequentially
and based on `0000-template.md`. "Significant" means one of the following: a new
framework-level dependency, a new architectural pattern, a change to the layering
rules, a persisted-format change, or anything that the SDD does not already cover.
Accepted ADRs are never edited, only superseded.

## Consequences
There is a small writing cost per decision, in exchange for a searchable history of *why*.
