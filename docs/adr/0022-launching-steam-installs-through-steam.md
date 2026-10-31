# ADR-0022: Launch Steam installs through Steam

- **Status:** Accepted
- **Date:** 2026-10-31

## Context
DAORework has always launched the game by starting `daorigins.exe` (or `DAOriginsLauncher.exe`) itself. On the
owner's real Steam install, during the 0.3.0 release run, that start was refused by Steam with *"Application load
error 3:0000065432"*. Starting the game through Steam is how Steam expects its games to start, so this ADR made
DAORework do that.

*Correction, same day:* the error came back when Steam started the game, and was caused by the 4 GB patch on Steam's
protected executable, not by how DAORework started it ([ADR-0023](0023-steam-protected-executable.md)). Launching
through Steam stays: it is still how Steam expects to be used, and it brings the overlay and playtime.

## Decision
- **A Steam install is launched by asking Steam to start it:** DAORework opens `steam://rungameid/47810`, the same
  thing a desktop shortcut made by Steam does. Steam starts the game its own way, with its checks, the overlay and
  playtime. When the profile has launch arguments, it opens `steam://run/47810//<arguments>/` instead, and Steam asks
  the player to confirm them.
- **What counts as a Steam install:** the profile's install folder is `<library>\steamapps\common\<folder>`, and the
  library's `steamapps\appmanifest_47810.acf` names that folder as its `installdir`. Nothing new is stored in the
  profile; a profile made before this change is recognised the same way.
- **Every other install is launched as before** (GOG, EA App, a copy placed by hand), starting the executable the
  profile names.
- For a Steam install the profile's launch target (game or official launcher) does not apply: Steam runs what its
  Play button runs.
- **Where the code goes** (Strategy, SDD §5): a new `SteamAwareGameRuntime`, registered for
  `RuntimeMode.OriginalGame`, chooses between launching through Steam and the unchanged `ProcessGameRuntime`. Opening
  a link is a new port, `IUriLauncher`, with the adapter `ShellUriLauncher` in Infrastructure; `SteamGameInstall`
  reads the app manifest. `LaunchResult.ProcessId` becomes optional, because Steam starts the game, not DAORework.

## Consequences
- Steam players launch from DAORework the way Steam expects, with the overlay and playtime.
- DAORework no longer knows the game's process id for a Steam launch; the window and the command line say that Steam
  was asked to start the game.
- If Steam is not installed or the link cannot be opened, the launch fails with `DAO_PROCESS_START_FAILED` and a hint
  to start the game from Steam.
- GOG and EA App installs keep the direct start; their real runs are still open (issue #5).

## Alternatives considered
- *Keep starting the executable and tell Steam players to start Steam first:* asks every player to work around
  DAORework, and does not cover the other causes of the error.
- *Start `steam.exe -applaunch 47810`:* needs the path to `steam.exe` and does the same as the link, with more to go
  wrong.
- *Store the store in the profile (schema 2 with a migrator):* more code for the same answer; the folder already says
  which store it came from.
