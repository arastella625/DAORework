# ADR-0013: Managed fixes: the install folder, patched in place with a backup

- **Status:** Accepted
- **Date:** 2026-09-30

## Context
ROADMAP P2-08 is ADR-0009's tier 2: DAORework applies two well-known fixes itself instead of only advising about them.
- The **4 GB patch** sets the large-address-aware flag in the header of `bin_ship\daorigins.exe`, so the 32-bit game
  can use up to 4 GB of memory instead of 2 GB. The community tool for this is NTCore's 4GB Patch.
- **DXVK** is a `d3d9.dll` placed in `bin_ship`. It runs the game's Direct3D 9 on Vulkan. DXVK is zlib-licensed and
  released on GitHub as a `.tar.gz` holding an `x32` and an `x64` folder.

Both fixes change the **install folder**, which ADR-0012 left out of its model: the record, the owned subfolders and
the `AddIns.xml` rules there are all about the documents folder. The install folder is different in four ways.
- It holds files DAORework did not put there and must never lose, starting with the game executable itself.
- Steam's *Verify integrity of game files*, and a reinstall, can put the original files back at any time.
- Under `Program Files`, Windows needs administrator rights to write there.
- A running game holds its executable and DLLs open.

The known-items catalog (P1-07, ADR-0011) already recognizes both fixes. It reads the flag from the PE header,
recognizes DXVK by its version string, and flags a 64-bit DLL the game cannot load. SDD §12 allows no network access
before Phase 3, so the fix files come from the user.

## Decision

### Same path as mods
Every fix goes through the Phase 2 path: plan, confirm, backup, journaled apply, record. The plan is shown first,
`--dry-run` stops there, and `--yes` is needed where the console cannot ask. The backup store, the journal and the
runner from P2-04 and P2-05 are reused. Where one of them is specific to the documents folder, a refactor PR makes it
general first, and changes no behavior.

### Where the state lives
- One record per install folder: `%LOCALAPPDATA%\DAORework\deployments\<installKey>.json`. It lists the fixes
  applied and, for every file a fix changed or placed, its path relative to the install folder, its SHA-256 before
  the fix (or "absent"), and its SHA-256 after.
- `installKey` is computed exactly like `documentsKey`: the first 16 hex digits of the SHA-256 of the install folder's
  full, normalized path. Backups go to `deployment-backups\<installKey>\<id>\`, and the journal sits beside the record.
  *(Amended 2026-09-30, P2-08 domain PR: the `install-` prefix first written here was dropped. The record, journal and
  backup stores are keyed by the folder's path, so the install folder gets its own record with no new store code.)*
- Fixes are not mods, so they are not in a documents folder's record or active list. `fixes status` reports them
  instead.

### The 4 GB flag: patched in place (chosen by the owner, 2026-09-30)
- `daorigins.exe` is backed up whole, then patched in place. The unpatched and the patched exe are both kept in
  `library\fixes\large-address-aware-<sha8>\`, named by the unpatched exe's hash, and the patch is applied by copying
  the patched one over the original like any other planned replacement. Two header fields change and nothing else: the
  `IMAGE_FILE_LARGE_ADDRESS_AWARE` bit in the COFF characteristics, and the optional header's `CheckSum`. The checksum
  is recomputed with the documented PE checksum algorithm when it was non-zero, and left zero when it was zero, as
  NTCore's tool does. The patched bytes are written to a temporary file beside the exe, which then replaces it.
- The patch refuses a file that is not a 32-bit (i386) PE image, and does nothing when the flag is already set. It
  never strips, unpacks or rewrites anything else in the file. A copy-protection stub, if there is one, stays exactly
  as it was, so nothing here works around DRM (LEGAL_BOUNDARIES.md).
- Steam, the official launcher and DAORework's own launch all start the same patched exe.

### DXVK: a release the user supplies
- `fixes apply dxvk <path>` accepts a DXVK release as downloaded (`.tar.gz`), a `.zip`, an extracted folder, or a
  `d3d9.dll` on its own. From an archive or folder DAORework takes `x32/d3d9.dll`. The file must match the catalog's
  DXVK fingerprint, with its version read from it, and must be a 32-bit PE image. Anything else is refused. `.tar.gz`
  is read with the BCL (`System.Formats.Tar`, `GZipStream`); nothing is extracted except that one file.
- The DLL is first copied to `library\fixes\dxvk-<version>-<sha8>\d3d9.dll`, so reapplying and rolling back never
  need the download again. It is then copied to `bin_ship\d3d9.dll`. No `dxvk.conf` is written.
- A `d3d9.dll` that is already in `bin_ship` is replaced, after a backup, only when the catalog recognizes it as
  DXVK: this covers an upgrade or a downgrade. Any other `d3d9.dll`, such as ReShade or another wrapper, blocks the
  plan with its name and location. DAORework never overwrites a graphics hook it cannot identify. A DXVK DLL placed by
  hand is kept in the library before it is replaced, so removing the new one can put it back.

### Removing a fix, and changes made outside DAORework
- `fixes remove <fix>` puts back the files that fix replaced, from the copies the library keeps of them (see above;
  the backup stays as the safety net). A file DAORework placed where nothing was before is deleted.
  *(Amended 2026-09-30, P2-08 domain PR: first written as "from its backup". A backup belongs to one change, so after
  a second fix change the first fix's original would be hard to find; the library copy is found by its hash.)* Both happen only where the file's current hash still matches the record's "after" hash.
- A recorded file whose hash no longer matches was changed outside DAORework, for example by Steam's verify or a
  reinstall. It is left in place and reported: `fixes status` says the fix is no longer in effect, and the plan to
  remove it shows the file as "changed since". Applying the fix again plans against what is on disk now.
- `fixes rollback` undoes the last fix change from its backup, as `mods rollback` does for mods.

### Write access and a running game (chosen by the owner, 2026-09-30)
- DAORework never asks for administrator rights and never relaunches itself elevated. Before planning, it checks it
  can write in `bin_ship` by opening a file already there for writing and closing it again, without writing.
  *(Amended 2026-10-01, Phase 2 exit gate: first written as "create and delete a file". The fault-injection gate
  (ADR-0014) killed DAORework between the two and found the check file left in the game folder; opening an existing
  file creates nothing.)* When it cannot, the plan is blocked with `DAO_FIX_INSTALL_NOT_WRITABLE`,
  and the recommendation is to run DAORework as administrator or move the install to a Steam library outside
  `Program Files`. Both executables declare a requested execution level (`asInvoker`) in their application
  manifest, which turns off Windows file virtualization, so a write is never silently redirected to `VirtualStore`.
  P2-08 adds the manifest where the build does not already produce one.
- A target file that cannot be opened for exclusive write, usually because the game is running, blocks the plan with
  `DAO_FIX_FILE_IN_USE`. Nothing is backed up or changed.

### Diagnostics
The P1-03 and P1-07 rules keep reporting what is on disk; a fix applied by hand and one applied by DAORework look the
same to them. Their recommendations now name the command, for example `fixes apply laa`. New codes use the
`DAO_FIX_*` area (SDD §8.3).

## Consequences
- P2-08 ships as separate PRs: this ADR; any refactor that makes the backup, journal or runner general; the 4 GB flag
  with the `fixes` command group; DXVK.
- The PE checksum algorithm and the flag change are pure functions over bytes, in Core. They are tested against
  checksums worked out independently, never against a copy of the game's executable (LEGAL_BOUNDARIES.md).
- Steam's verify or a game update can undo a fix at any time. The record and `fixes status` make that visible instead
  of hiding it, and applying the fix again is always safe.
- Installs under `Program Files` need the user to run DAORework as administrator. This is a deliberate limit; an
  elevated helper would need its own ADR.
- If the exe is Authenticode-signed, the patch invalidates its signature. Windows does not stop an unsigned exe from
  running, and the plan says so when a signature is present.
- Fetching DXVK from GitHub is still out. It needs its own ADR, and SDD §12 allows network access only from Phase 3,
  with the user's opt-in.

## Alternatives considered
- *A patched copy beside the exe (`daorigins-4gb.exe`):* Steam and the official launcher would still start the
  unpatched exe, so the fix would only apply when launching through DAORework.
- *Clearing the flag to remove the patch:* restoring the backed-up original is exact, including the checksum and
  anything a newer exe differs in.
- *Asking for elevation (a UAC helper):* a second process and a privilege boundary to secure and test, for installs
  that can also be moved.
- *Replacing any `d3d9.dll`:* it could silently remove a user's ReShade or another wrapper.
- *Deploying DXVK straight from the download:* rollback and reapply would need the original archive to still exist.
