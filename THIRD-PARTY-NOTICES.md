# Third-party notices

Code ported into this repository from another project, and fonts and art shipped inside
the App, are listed here with their license, as ADR-0009, ADR-0016 and ADR-0026 require. The NuGet
packages compiled into a release are listed, with their license texts, in the
`THIRD-PARTY-LICENSES.md` that `scripts/third-party-licenses.ps1` generates into every
release zip.

| Component | Source | License | Where used |
|---|---|---|---|
| Cinzel font (`src/DAORework.App/Assets/Fonts/Cinzel.ttf`, the variable `Cinzel[wght].ttf`) | [NDISCOVER/Cinzel](https://github.com/NDISCOVER/Cinzel), from [google/fonts](https://github.com/google/fonts/tree/main/ofl/cinzel) | SIL Open Font License 1.1 (`src/DAORework.App/Assets/Fonts/OFL.txt`, `Cinzel-OFL.txt` in the release zip) | Page, section and dialog headings in the App |
| Kenney *Fantasy UI Borders* 1.0, one border (`src/DAORework.App/Assets/Kenney/panel-border-001.png`) | [kenney.nl](https://kenney.nl/assets/fantasy-ui-borders), [itch.io](https://kenney-assets.itch.io/fantasy-ui-borders) | CC0 1.0 (`src/DAORework.App/Assets/Kenney/License.txt`, `Kenney-License.txt` in the release zip); credit is not required but given | The frame around Home's readiness and suggestion cards, tinted by the theme ([ADR-0026](docs/adr/0026-workflow-first-layout.md) §5) |
