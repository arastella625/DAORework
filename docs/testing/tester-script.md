# Trying DAORework: a script for testers

Thank you for trying DAORework on your own game. This takes about 30 minutes. You don't need to know how DAORework
works: the point is to see where a player gets stuck. Write down anything that confused you, even if you worked it out
(P2.9-06).

**Before you start:** copy your documents folder (`Documents\BioWare\Dragon Age`) somewhere safe, outside DAORework,
so nothing depends on DAORework's own backups. Close the game.

Use the latest zip from [GitHub](https://github.com/arastella625/DAORework/releases/latest) or
[Nexus Mods](https://www.nexusmods.com/dragonage/mods/7083). Unzip it anywhere and run `DAORework.exe`.

## 1. Your first game with mods

| # | Do this | Then tell us |
|---|---|---|
| 1 | Open DAORework and find your game | Did it find the game and your documents folder by itself? If not, what did you have to pick? |
| 2 | Look at Home without clicking anything | Is your game ready to play? How can you tell? |
| 3 | If Home offers a next step, take it | What did it ask, and did you know what to answer? |
| 4 | Add one mod you know, such as a hair or face mod: *Add mods*, or drop the file on the window | Anything that surprised you |
| 5 | Turn it on, read the preview, and *Apply* | What did the mod add? Where did you see that? |
| 6 | Press *Play* and load a save, or start a new character if it's a character-creation mod | Was the mod there in the game? |
| 7 | Back in DAORework, *Undo* that change (Home's *Last change*, or History) | Is the mod gone from the game? |

## 2. If you already have mods

Only if you installed mods by hand or with Vortex before:

| # | Do this | Then tell us |
|---|---|---|
| 8 | Follow Home's next step to take over what's in your game (*Manage*, or *Move from Vortex*) | Did every mod you have show up? Was anything offered that isn't a mod? |
| 9 | Start the game and load a save | Does it play as it did before? |
| 10 | Try *Find the problem mod* for one or two rounds, then *Put everything back* | Did you understand what each answer meant? |

## 3. Steam copies with Steam's protected program

Only from DAORework 0.7.0, and only on Steam. If DAORework says your copy's `daorigins.exe` is protected (Fixes says
the 4 GB patch can't go on it), these two have never been tried
([ADR-0033](../adr/0033-mods-that-run-inside-the-game.md), *Not yet seen*):

| # | Do this | Then tell us |
|---|---|---|
| 11 | Add [DAO Lib](https://www.nexusmods.com/dragonage/mods/7052), turn it on (tick *I trust this mod and where I got it*), then start the game through DAORework | Did the game reach the main menu? |
| 12 | In Steam, *Properties* → *Installed files* → *Verify integrity of game files*, then press *Check the game* in Settings › Troubleshooting | Is DAO Lib still on, or does DAORework say a file went? |

Then turn DAO Lib off, or *Undo* it.

## Sending it back

- **GOG or EA App:** reply on [#5](https://github.com/arastella625/DAORework/issues/5).
- **Steam, or anything else:** open a [new issue](https://github.com/arastella625/DAORework/issues/new/choose), or post
  in the Nexus page's *Posts* tab.

Say which copy of the game you have (Steam, GOG or EA App) and which DAORework version: the zip's name says it, such
as `DAORework-0.7.0-win-x64.zip`. Then give your answers, by step number.

If something went wrong, Settings › Troubleshooting has *Save a support bundle*. It holds folder paths, which can
include your Windows user name, so look through it before you attach it.
