# ADR-0019: Playing the game's own music from the user's install

- **Status:** Proposed. Two questions must be settled before it can be accepted: the owner's decision on the user
  agreement (question 1) and a format check on a real install (question 2).
- **Date:** 2026-10-31

## Context
On 2026-10-31 the owner asked for music in the manager while people work: random snapshots of Dragon Age: Origins'
own soundtrack, taken from the game the user already has installed. This would sit beside P3-05 (a music player
for the user's own files, which plays a soundtrack Steam installed when there is one).

The first idea was a mod that DAORework installs into the game. That cannot work. An add-in only runs while the
game runs, and the game's script engine has no way to read files or talk to another program. Besides, the manager
is usually open while the game is not. Only the manager can do this, by reading the music files that are already in
the install.

Three things constrain it:
- **No music is shipped.** LEGAL_BOUNDARIES forbids including the game's music, and DAORework only references
  local files already on the user's machine. This ADR would be the first time DAORework *plays* game content outside
  the game, not just reads its files to diagnose or install mods.
- **No circumvention or proprietary code.** LEGAL_BOUNDARIES forbids circumventing DRM or ownership checks, and
  decompiled or copyleft code. Any reader has to be clean-room work from public format documentation (ADR-0009
  tier 3).
- **The format is not yet confirmed.** The game is believed to keep its audio in FMOD sound banks (`.fsb`, with
  `.fev` event files) under `packages\core\audio\`, played through the `fmodex.dll` it ships. Nobody has checked
  this on a real install, or whether the banks are encrypted, or which codec the music uses.

## Decision
DAORework may play the game's own music only under all of the rules below, and only once questions 1 and 2 are
settled and recorded here. Until then, nothing in this ADR is built. P3-05 goes ahead with the user's own files and
an installed soundtrack, which need neither question settled.

### Rules if it is built
1. **Read in place, decode in memory.** Music is read from the install folder of the selected profile. Nothing is
   copied, cached on disk, converted to files, exported, uploaded or shipped. There is no *save this clip*, now or
   later. Turning the feature off, or uninstalling DAORework, leaves no game audio anywhere.
2. **Music only.** Only banks identified as music are played; dialogue, voice and sound effects never are. The
   list of music banks is data in the repository (file names and sizes from a real install, like the known-items
   catalog), not audio.
3. **Snapshots, not a ripper.** A snapshot is a random excerpt of 30 to 90 seconds from a random music track,
   faded in and out, with a pause between snapshots. The feature is off by default, has an on/off switch and a
   volume, and pauses while the game runs. It shares P3-05's player and settings.
4. **Plain files only.** If the music banks are encrypted or obfuscated, the feature is not built. DAORework does
   not decrypt, look up or embed keys, or work around any protection, whatever it is called.
5. **Our own reader.** The bank container is read by DAORework's own code, written from public format
   documentation, in Core (parsing) and Infrastructure (file access). No FMOD SDK, no loading the game's
   `fmodex.dll`, no code from GPL projects. Decoding the codec inside (MP3, PCM or ADPCM, whatever question 2 finds)
   uses a permissively licensed decoder or the operating system's own, checked under ADR-0009 and recorded in
   `THIRD-PARTY-NOTICES.md`. Playback uses the audio library P3-05's ADR chooses.
6. **The user agrees in the app.** Turning it on the first time says what it does (reads the music in your Dragon
   Age install and plays excerpts, never copying them) and asks the user to confirm.

### Question 1: the user agreement (the owner decides)
Game user agreements often forbid reverse engineering, or using the game's content other than to play the game.
Whether reading the music banks to play excerpts in a companion tool falls under such a clause depends on the actual
wording for each edition the project supports (Steam, GOG and the EA app). It may also depend on
the law where the user lives. This is a legal judgement, not an engineering one, and this ADR does not make it.
The options:
- **a. Don't build it.** P3-05 covers the wish legitimately: the official soundtrack, where the user owns it, and
  any music the user has.
- **b. Build it under the rules above,** after the owner has read the agreements and records here that they are
  comfortable with it, ideally after asking someone qualified.
- **c. Ask first.** Ask EA for permission, or get legal advice, before deciding between a and b.

Recommendation from the engineering side: settle this before any format work, because the answer may make that work
moot. Until then, a plus P3-05 delivers music in the manager at no risk.

### Question 2: the format on a real install (the owner runs a check)
This read-only PowerShell 7 snippet lists the audio files in an install and the first four bytes of each. It changes
nothing and sends nothing anywhere. Paste the output into the review of this ADR.

```powershell
$install = 'D:\SteamLibrary\steamapps\common\Dragon Age Ultimate Edition'   # your install folder
Get-ChildItem -Path $install -Recurse -File -ErrorAction SilentlyContinue |
  Where-Object { $_.FullName -match '\\(audio|sound|music)\\' -or $_.Extension -in '.fsb', '.fev', '.fdp' } |
  ForEach-Object {
    $stream = [IO.File]::OpenRead($_.FullName)
    try { $head = [byte[]]::new(4); [void]$stream.Read($head, 0, 4) } finally { $stream.Dispose() }
    [pscustomobject]@{
      File  = $_.FullName.Substring($install.Length + 1)
      MB    = [math]::Round($_.Length / 1MB, 1)
      Magic = -join ($head | ForEach-Object { if ($_ -ge 32 -and $_ -lt 127) { [char]$_ } else { '.' } })
    }
  } | Sort-Object File | Format-Table -AutoSize | Out-String -Width 300
```

What the output means:
- **`FSB4` or `FSB5` at the start of the music banks:** plain banks. The format work can be scoped from the codec
  flags in their headers.
- **Anything else at the start of an `.fsb`:** the bank is encrypted or wrapped. Under rule 4 the feature stops
  there.
- **No `.fsb` files, or music in another container (an `.erf`, for instance):** this ADR is rewritten around what
  is found before going further.

## Consequences
- Nothing changes until this ADR is accepted. P3-05 is not blocked by it and keeps its own ADR for the audio
  library.
- If accepted, the feature is Phase 3 work alongside P3-05, not Phase 2.5, unless the owner moves it.
- If accepted, DAORework owns a bank reader and has to keep it working across the editions it supports. The music
  bank list must be checked on each edition, as the known-items catalog is.
- P3-05 needs a check that the game is running, so its player can pause. That check does not exist yet, and this
  feature would use it too.
- If question 1 is answered *a*, this ADR moves to *Deprecated* with that reason, and the P3-05 entry in the
  ROADMAP keeps saying the game's own audio packages are out of scope.

## Alternatives considered
- **A mod installed into the game that sends music to the manager:** impossible. Add-ins run only inside the game,
  and its scripts cannot read files or talk to other programs.
- **Loading the game's own `fmodex.dll`:** it is a proprietary library licensed for the game, and a 32-bit DLL that
  DAORework's 64-bit process cannot load.
- **The FMOD SDK:** a proprietary dependency in a project whose source is public. It does not help with encrypted banks either, and
  rule 4 rules those out anyway.
- **Extracting the music to files once and playing those:** makes copies of the game's content on disk, which rule
  1 forbids. It is also the step towards a ripper that this ADR keeps out.
- **Shipping the soundtrack, or downloading it:** forbidden by LEGAL_BOUNDARIES.
- **Only the soundtrack Steam installs under `steamapps\music\`:** kept. It is P3-05's route, and it is the legal way
  to have the game's music outside the game. It only covers users who own that soundtrack.
