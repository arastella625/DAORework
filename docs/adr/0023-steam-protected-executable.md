# ADR-0023: Do not apply the 4 GB patch to Steam's protected executable

- **Status:** Accepted. Narrows [ADR-0013](0013-managed-fixes-in-the-install-folder.md) for Steam installs.
- **Date:** 2026-10-31

## Context
On the owner's real Steam install, the game would not start after DAORework applied the 4 GB patch: Steam stopped it
with *"Application load error 3:0000065432"*, whether DAORework started the executable or asked Steam to start it
([ADR-0022](0022-launching-steam-installs-through-steam.md)). Removing the patch fixed it; applying it again brought
the error back.

Steam's `daorigins.exe` is wrapped in Steam's copy protection (SteamStub), which adds a `.bind` section and refuses
to run once the file has been changed. The patch changes one flag and the checksum, exactly as NTCore's 4GB Patch
does, and that is enough. Steam players who use the 4 GB patch first remove the protection (with Steamless, or by
downloading an already-decrypted `daorigins.exe` from Nexus), then patch. DAORework cannot do either:
[LEGAL_BOUNDARIES.md](../LEGAL_BOUNDARIES.md) rules out circumventing DRM and distributing game files.

## Decision
- **Detection only:** `SteamStub` reads an executable's section table and recognises the `.bind` section. Nothing
  is decrypted, removed or replaced.
- **The 4 GB fix refuses a protected executable** (`DAO_FIX_STEAM_PROTECTED`), with the reason, and points to DXVK,
  which helps with the game's memory crashes without touching the executable.
- **Diagnostics:** for a protected executable, `DAO_LAA_DISABLED` is not reported (so Next steps no longer offers the
  patch); `SteamProtectedExecutableRule` reports instead `DAO_STEAM_EXE_PROTECTED` (info) for an untouched copy, or
  `DAO_STEAM_EXE_PATCHED` (warning, not blocking) when the copy carries the 4 GB flag anyway, with how to undo it:
  remove the patch on the Fixes tab, or Steam's *Verify integrity of game files*.
- **An executable without the `.bind` section** (GOG, EA App, a copy placed by hand, or one the player replaced
  themselves) is treated as before. DAORework neither supplies nor recommends a replacement executable.

## Consequences
- Steam players are no longer led into a patch that stops Steam starting the game, and one who already applied it
  is told how to undo it. Removing a patch DAORework applied still works from the Fixes tab.
- Steam players do not get the 4 GB benefit from DAORework. DXVK stays available to them.
- The README, CHANGELOG, Nexus page and website say the 4 GB patch is for copies without Steam's protection.

## Alternatives considered
- *Keep the original executable beside the patched one, or start a patched copy under another name:* Steam always
  starts `daorigins.exe` and checks it; anything that gets a changed copy past the check circumvents the DRM.
- *Decrypt with Steamless, or download a decrypted executable for the player:* DRM circumvention and game-file
  distribution, both ruled out by LEGAL_BOUNDARIES.md.
- *Keep offering the patch with a warning:* it breaks the game for most players, on the store most of them use.
