# Third-party notices

Code ported into this repository from another project, and fonts shipped inside the
App, are listed here with their license, as ADR-0009 and ADR-0016 require. The NuGet
packages compiled into a release are listed, with their license texts, in the
`THIRD-PARTY-LICENSES.md` that `scripts/third-party-licenses.ps1` generates into every
release zip.

| Component | Source | License | Where used |
|---|---|---|---|
| Cinzel font (`src/DAORework.App/Assets/Fonts/Cinzel.ttf`, the variable `Cinzel[wght].ttf`) | [NDISCOVER/Cinzel](https://github.com/NDISCOVER/Cinzel), from [google/fonts](https://github.com/google/fonts/tree/main/ofl/cinzel) | SIL Open Font License 1.1 (`src/DAORework.App/Assets/Fonts/OFL.txt`, `Cinzel-OFL.txt` in the release zip) | Page, section and dialog headings in the App |
