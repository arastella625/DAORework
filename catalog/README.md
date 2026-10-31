# The known-items catalog

`known-items.json` lists the community fixes and mods DAORework recognizes, and the combinations known to break
([ADR-0009](../docs/adr/0009-third-party-mods-and-tools.md) tier 1, [ADR-0011](../docs/adr/0011-known-items-catalog.md)).
It also lists BioWare's official DLC, so that adopting hand-installed mods never offers one
([ADR-0015](../docs/adr/0015-adopting-hand-installed-mods.md)). It is embedded in the release; `KnownItemsRule` reports
the fixes and mods on it in every diagnostic run, and leaves the DLC to `DAO_DLC_FOUND`. Nothing listed here is
downloaded, installed or redistributed.

## The rule for adding an entry

An entry is added only with a **verifiable fingerprint** and a **source link**. "Verifiable" means someone else can
check it against the item's own download: a UID read out of its `.dazip` manifest, a file name out of its archive, or
a byte string out of its binary. Conflicts need a link to where the incompatibility is stated (the mod's own page or
readme), not a forum guess.

## Entry fields

| Field | Meaning |
|---|---|
| `id` | Stable, lowercase, hyphenated. Conflicts refer to it; never change it once released. |
| `name` | What the user knows it as. |
| `kind` | `Fix`, `Mod`, `Tool` or `Official` (BioWare's own DLC). |
| `source` | An `https` link to the item's page. |
| `license` | The item's license as its author states it. |
| `fingerprints` | One or more; the item is found when any one matches. |
| `whenMissing` | Optional. Advice shown when the item is **not** installed; leave it out for items that are only reported when found. |

Fingerprint kinds:

| `kind` | `value` | Found when |
|---|---|---|
| `AddInUid` | The `UID` of the `AddInItem` in the `.dazip`'s `Manifest.xml` | `Settings\AddIns.xml` registers that UID (enabled or not) |
| `OverrideFile` | A file name the mod ships | That file is anywhere under `packages\core\override` |
| `GameFile` | A path relative to the install folder, with `/` | The file exists and, with `marker`, contains the marker as a whole NUL-terminated string. The string after it is reported as the version. |
| `LargeAddressAware` | The executable, for the record | The game executable's header has the flag set |
| `DownloadSha256` | The SHA-256 of an official download, in lowercase hex, with its `version` | Never on disk. At import, a download with this hash is shown as that version of the item; a download the item's other fingerprints recognize but that matches none of its hashes gets `DAO_MOD_SAFETY_DOWNLOAD_DIFFERS` ([ADR-0018](../docs/adr/0018-mod-safety.md)) |

A conflict is `{ "first": id, "second": id, "severity": "Warning" | "Info", "reason": "...", "source": "https://..." }`.
Conflicts never use `Error`: Phase 1 only reports, it never blocks a launch.

## Getting the fingerprints

Download the mod (nothing needs installing) and run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\catalog-fingerprint.ps1 -Path "C:\path\to\downloads"
```

It prints each `.dazip`'s `AddInItem UID` and `Name` (also for a `.dazip` inside a Nexus `.zip`), the file list of
plain archives and extracted folders, and each archive's SHA-256. Put the UID or a distinctive file name in the entry,
and the archive name and hash in the pull request so a reviewer can check them. Add the hash to the entry as a
`DownloadSha256` fingerprint with the version, and keep the older versions' hashes: a player may still have one. An
entry always keeps a fingerprint found on disk, because a download hash cannot tell whether the item is installed.

## Where the current fingerprints come from

| Entry | Fingerprint | Read from |
|---|---|---|
| `qwinn-fixpack` | `AddInUid` `qwinn_fixpack_3`, `DownloadSha256` of v3.5 | `QUDAO Fixpack v3.5-4689-3-5-1550475932.zip` (Nexus mod 4689), sha256 `6a2788e24d900bea771a27353f17e0f88dd96914175c731f385504a3155f2674`, via the `.dazip` inside it |
| `extra-dog-slot` | `AddInUid` `Extra_Dog_Slot`, `DownloadSha256` of v1.41 | `dao_extradogslot_1_41-664.dazip` (Nexus mod 664, v1.41), sha256 `973f9879f5301581adb922c14c89581d97c3a30b8b90151a3e9cfa4e784cb8d2` |
| `dxvk` | `GameFile` `bin_ship/d3d9.dll`, marker `DXVK: ` | See below |
| `dlc-*` (11 entries) | `AddInUid` `DAO_PRC_*` (19 UIDs) | See below |

The list of conflicts is empty until the mods the Fixpack's documentation calls incompatible (Alistair Dialog Patch,
Zevran Dialogue Fixes, Equal Love, Complete Bi Overhaul, Auto Loot, Improved Atmosphere) have fingerprints read the
same way. A conflict needs both of its sides in the catalog.

The DXVK marker was read from the `x32` and `x64` `d3d9.dll` of releases 1.10.3, 2.3 and 3.1.1: each contains
`DXVK: ` followed by the version (`v2.3`) as separate NUL-terminated strings, from `Logger::info("DXVK: ", DXVK_VERSION)`
in `src/dxvk/dxvk_instance.cpp`.

The official DLC UIDs were read on 2026-10-29 from `Settings\AddIns.xml` of the owner's Steam Ultimate Edition
install: every `AddInItem` with `BioWare="1"`, 19 in all. Anyone with that edition can check them in their own
`AddIns.xml`. On that install the DLC folders are in the install folder's `Addins`, not under Documents, so ADR-0015's
rule that an add-in whose folder is not under Documents is never a candidate also keeps them out. Each entry is one
DLC; an add-in whose UID adds `_DAO` or `_EP1` to the DLC's own is that DLC's item pack for Origins or for Awakening
(Witchcraft, Blightblood, Golem's Might, Battledress of the Provocateur) and is listed with it. Feastday Gifts and
Feastday Pranks are separate add-ins and separate entries. An official add-in that was not on that install is missing
until its UID is read from a real install the same way.

## Tests

`DAORework.Infrastructure.Tests` loads the shipped catalog and fails if it does not validate, so a malformed entry
fails CI, not a user's diagnostics.
