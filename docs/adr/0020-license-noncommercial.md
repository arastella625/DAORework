# ADR-0020: License the project under PolyForm Noncommercial 1.0.0

- **Status:** Accepted. Supersedes [ADR-0008](0008-project-license-mit.md).
- **Date:** 2026-10-31

## Context
ADR-0008 chose the MIT License. MIT lets anyone sell DAORework, or a copy of it with a new name, as long as they keep
the copyright notice, and no Nexus permission setting can take that back. On 2026-10-31, while setting the Nexus
permissions, the owner decided that nobody may sell it: DAORework is and stays free, and others may build on it for
free too.

The change has to happen now. The repository is still private and nothing has been published outside it, so no copy
is out there under MIT. The owner is the only copyright holder; there are no outside contributors.

## Decision
- DAORework is licensed under the **PolyForm Noncommercial License 1.0.0** (`LICENSE` at the repository root, with
  the line `Required Notice: Copyright (c) 2026 Amber Rastella`). Anyone may use, copy, change and share it, and make
  new works from it, for any noncommercial purpose: personal use, hobby projects, other free mods and tools, charities
  and schools. Selling it, or using it for any commercial purpose, is not allowed.
- Anyone who passes it on must pass on the license terms (or their URL) and the Required Notice line.
- Contributions are accepted under the same license (CONTRIBUTING.md), so the owner can keep the terms consistent.
- DAORework is described as **free and source-available**, never as "open source": the Open Source Definition does
  not allow a ban on commercial use, and claiming the label would mislead.
- The Nexus permissions match: uploading elsewhere, changing it and converting it are allowed with credit; use in
  anything sold is not ([docs/release/nexus/permissions.md](../release/nexus/permissions.md)).

## Consequences
- Nobody may sell DAORework or a derivative of it. Enforcing that is up to the owner; the license gives her the
  grounds, and Nexus removes files that break an author's stated permissions.
- Dependencies are unaffected: Avalonia, CommunityToolkit, SharpCompress, Serilog and the rest are MIT or Apache 2.0,
  and Cinzel is SIL OFL, all of which may be shipped inside a noncommercial project with their notices
  (`THIRD-PARTY-LICENSES.md` in the release zip).
- ADR-0009's tiers still hold: permissively licensed code (DAOTools, MIT) may be ported with its notice; GPL code
  is never copied.
- Another free mod manager can still reuse DAORework's code; a commercial launcher cannot embed it, which is the
  point. Companies cannot use it in paid products either.
- A license covers what copyright covers. Parts written with AI assistance may have thinner copyright protection in
  some countries; the owner's design, selection, review and edits are hers. This does not change the choice.
- README, CONTRIBUTING, the SDD, the release zip's `THIRD-PARTY-LICENSES.md`, CHANGELOG and the Nexus page say the
  new license. The 0.1.0 and 0.2.0 tags were built under MIT, but were never published outside the private
  repository.

## Alternatives considered
- *Keep MIT:* allows selling, which the owner rules out.
- *GPL-3.0:* anyone selling it must share their source, which discourages selling but still allows it.
- *Creative Commons BY-NC-SA 4.0:* forbids selling, but Creative Commons itself advises against its licenses for
  software (no patent terms, unclear on source and binaries).
- *MIT with the "Commons Clause":* a non-standard addition whose wording is widely criticized as ambiguous.
- *PolyForm Strict 1.0.0:* also forbids changes and new works, which would stop others building on DAORework, against
  the owner's wish that it be a foundation others can take further.
