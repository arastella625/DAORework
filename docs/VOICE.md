# How DAORework talks

The words in the window ([ADR-0026](adr/0026-workflow-first-layout.md)). Every view, status line, error and plan
summary follows these rules. The command line keeps its own words; see the end.

## The six rules
1. **Talk about the player's game, not the code.** Say what happens to their game: *Turn on Long Hair*, not
   *Activate long-hair-f7b55346*. Name mods by their names, never their ids.
2. **Details only when asked.** Ids, file lists, sizes, folder paths and diagnostic codes go behind *Details*, in a
   tooltip, or in *Copy details for a bug report*. The first line is what a player needs to decide.
3. **Say each reassurance once.** The footer says *Every change can be undone*. A preview does not repeat that a
   backup is taken, and a panel does not open with a paragraph of explanation.
4. **Let messages go.** A success message fades after a few seconds and says what happened (*Turned on Blue Hair.*).
   An error stays until it is dealt with, and its first button fixes it or shows how.
5. **Write as a person does.** *1 file*, *2 files*, never *file(s)*. *2 minutes ago*, *Today, 18:05*,
   *Yesterday, 21:40*: local time, never UTC. Sizes rounded (*2.1 MB*), and only where they matter.
6. **One main button per screen.** It names the action (*Apply*, *Back up now*, *Set up DXVK*), never *OK* or
   *Submit*. Everything else is a plain button or a link.

## Words

| Say | Not | Notes |
|---|---|---|
| Turn on / Turn off | Activate / Deactivate, Apply / Remove | For a mod or a fix. The switch says *On* or *Off* beside it. |
| Manage | Adopt | Taking over a mod the player installed by hand. |
| Found in your game | Installed by hand, unmanaged | A tag on the mod, and a filter. |
| Undo, Undo to here | Roll back, Roll back to before this | *Undo* is always confirmed first. |
| History | Changes, backups | The list of changes. A save backup stays a *backup*. |
| Preview | Plan | What a change will do, shown before *Apply*. |
| Arrange | Load order, priority | *When two mods change the same file, the lower one wins.* |
| Add mods | Import | Adding never changes the game. |
| Play | Launch game | The one large button. |
| Add-in | addin, DAZIP mod | A packaged `.dazip` mod; the game lists it under Downloadable Content. |
| Override mod | override folder mod | Files that replace the game's own. |
| Combined files | merged files, conflicts | Files several mods need at once. |
| Your game folder, your documents folder | install directory, `UserDocumentsDirectory` | The folder names appear only in *Game folders*. |
| Check the game, Check again | Run diagnostics | Diagnostics are *Troubleshooting* in Settings. |
| Copy details for a bug report, Save a support bundle | Export support bundle | Copying comes with the Settings move (P2.5-27). |

## Patterns

**A preview**: a title that is the action, one line of what it changes, any warning, then the buttons.
> **Turn on Blue Hair**
> Copies 3 files into your game.
> ⚠ Red Hair and Blue Hair both change hair.dds. Red Hair's is used, since it's lower in the order.
> [Apply] [Cancel] Details

**A success**: what happened, in the past tense, with the thing's name. Optionally *Undo*.
> Turned on Blue Hair. · Undo

**An error**: what went wrong for the player, then what to do; the code goes in the details.
> Couldn't check the 4 GB patch: the game file couldn't be read. Make sure the game isn't running, then *Check again*.
> Details: `DAO_LAA_UNREADABLE`

**An empty list**: what will appear here, and how to get it.
> No changes yet. When you turn a mod or fix on or off, it shows here.

**A confirmation**: the question names the thing; the buttons name the choices.
> Undo back to before "Turned on Long Hair and Short Hair"? · [Undo] [Keep it]

## Tone
- Friendly and brief. A short sentence beats a long one.
- No blame (*couldn't*, not *you failed to*), no exclamation marks, no jokes in errors.
- A little flavour is welcome where nothing is at stake (*Nothing needs you. Enjoy the game.*), never in a
  warning or a preview.
- Red is for danger only (ADR-0016): a warning is amber, a success green, the rest the theme's own colours.

## The command line
The CLI keeps `mods activate`, `mods adopt`, `mods rollback` and its other verbs, since scripts and guides use them.
Its help text names the window's word where they differ (*`mods adopt`: manage mods found in your game*). Its output
may stay more detailed, since people who use it ask for detail.
