# Legal and Project Boundaries

DAORework is intended as an unofficial compatibility, launcher, diagnostic, and experimental runtime research project.

## Do not include

- BioWare/EA game assets
- Dragon Age: Origins dialog, scripts, music, textures, models, cinematics, or story content
- Decompiled proprietary source code
- Circumvention of DRM or platform ownership checks
- Any assets copied from community mods without permission

## Safer project rule

The user must own and install Dragon Age: Origins separately. DAORework should only reference local files already present on the user's machine.

## Clean-room direction

When the project eventually researches runtime replacement behavior, document findings at the behavior/specification level and avoid copying proprietary implementation details.

## Third-party code and content

- DAORework is licensed under PolyForm Noncommercial 1.0.0 (ADR-0020, superseding ADR-0008): free for any noncommercial use, never sold. Code ported from another project must carry a compatible permissive license (MIT, BSD, zlib, Apache-2) and is recorded in `THIRD-PARTY-NOTICES.md` with its source and license.
- Code from GPL or otherwise copyleft projects is never copied. Their documentation and observed behavior may inform clean-room work.
- Community mods and fixes are never bundled or redistributed. DAORework detects them, advises about them, and, from Phase 2, installs files the user supplies (ADR-0009).
