<!-- The release job fills this in and puts it in the run's summary (P2.6-07, docs/release/nexus-upload.md). -->
**To:** support@nexusmods.com

**Subject:** Review request: DAORework {VERSION} quarantined (Dragon Age: Origins, mod 7083)

Hello,

My upload "DAORework {VERSION}" (the main file on https://www.nexusmods.com/dragonage/mods/7083, file id
[FILE ID from the quarantine notice]) was quarantined by the automated checks. It is a mod manager, so it contains
compiled code: DAORework.exe (the app) and dao.exe (its command line). There is no nested archive in it.

- Source code: https://github.com/arastella625/DAORework
- The same zip on GitHub: https://github.com/arastella625/DAORework/releases/tag/v{VERSION}
  SHA-256: {SHA256}
- How it is built: by GitHub Actions from the tagged source (.github/workflows/release.yml), never on my own PC. The
  release has a GitHub build provenance attestation, which proves the zip came from that workflow and that commit:
  `gh attestation verify DAORework-{VERSION}-win-x64.zip --repo arastella625/DAORework`
- {SIGNED}
- To build it yourself: install the .NET 10 SDK, then from the repository root run `pwsh ./scripts/publish-release.ps1`.
  The zip lands in `./artifacts`. The steps are in CONTRIBUTING.md.

The app works offline: it never downloads anything and sends nothing over the internet. It does not include any game
files and does not touch Steam's DRM.

If any antivirus engine flagged it, could you tell me which ones and the detection names, so I can report a false
positive to them?

Thank you,
Amber
