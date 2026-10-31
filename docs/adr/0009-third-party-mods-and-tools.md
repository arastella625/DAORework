# ADR-0009: How DAORework incorporates existing mods, fixes and tools

- **Status:** Accepted
- **Date:** 2026-09-29

## Context
The community already has answers to several problems on the roadmap. Players
apply the large-address-aware flag, the DXVK Direct3D wrapper for the engine's
memory leak, and Qwinn's Ultimate Fixpack by hand. Mod managers exist
(DAO-Modmanager, Vortex, Mod Organizer 2), and the widely reported failure mode
is a mangled AddIns manifest that silently disables DLC. Format tools exist too:
DAOTools (MIT, reads and writes ERF and GFF) and xoreos (GPL-3, an open engine
whose Dragon Age: Origins support has stayed at "areas load, no gameplay" since
before its last release in 2020).

The project must decide what "incorporating" these means, within the limits of
LEGAL_BOUNDARIES.md and ADR-0008.

## Decision
Existing work is incorporated in three tiers. Each tier is a phase gate: a
tier is not built before the phase that lists it.

1. **Detect and advise (Phase 1).** A curated **known-items catalog** ships in
   the repository as data: for each fix or mod, its name, file fingerprints,
   source link, license, and known conflicts. Diagnostic rules use it to report
   what is installed and what is wrong (flag not set, DXVK present or missing,
   Fixpack alongside a known-incompatible mod). Nothing is redistributed.
2. **Manage (Phase 2).** Apply the large-address-aware flag and install
   permissively licensed fixes such as DXVK through the same plan → confirm →
   backup → apply → verify path as mod activation. Files come from the user
   until a separate ADR permits fetching them, because SDD §12 forbids network
   access before Phase 3.
3. **Reuse code (Phase 3).** Code may be ported from MIT or equally permissive
   sources with attribution in `THIRD-PARTY-NOTICES.md`. From GPL sources only
   documentation and observed behavior may be used, never code. Existing mod
   managers are studied for behavior, not copied.

For the remaster track (open decision D-1), the state of xoreos is the
reference point: an open engine is a multi-year effort even on its shoulders.
If that path is ever taken, the project contributes to xoreos rather than
starting a third engine.

## Consequences
Phase 1 gains a catalog task and Phase 2 a managed-fixes task (ROADMAP P1-07,
P2-08). The catalog needs a maintenance rule: an entry is added only with a
verifiable fingerprint and a source link. Fetching fixes needs its own ADR.

## Alternatives considered
- *Bundle the fixes:* redistributes third-party binaries and content, which
  LEGAL_BOUNDARIES.md rules out and most mod licenses forbid.
- *Wrap an existing mod manager:* inherits their manifest handling, which is
  the very problem to solve.
- *Ignore existing work:* wastes proven fixes and re-derives known conflicts.
