# ADR-0011: The known-items catalog: embedded JSON and one matcher per fingerprint kind

- **Status:** Accepted
- **Date:** 2026-09-29

## Context
ADR-0009 tier 1 calls for a curated catalog of community fixes and mods, kept
as data, that diagnostic rules use to report what is installed and which
combinations are known to break (ROADMAP P1-07). Its maintenance rule: an entry
is added only with a verifiable fingerprint and a source link. The SDD does
not say where such data lives, how it is loaded, or how the different ways of
recognizing an item on disk are organized.

Items are recognized in different ways. A `.dazip` add-in registers a UID in
`Settings\AddIns.xml`. An override mod is a set of files under
`packages\core\override`. DXVK is a `d3d9.dll` in `bin_ship`, and a byte string
inside it gives its version (verified on the 1.10.3, 2.3 and 3.1.1 releases).
The 4 GB patch is a flag in the game executable's header.

## Decision
- The catalog is one JSON document, `catalog/known-items.json`, with a
  `schemaVersion`. It is embedded in `DAORework.Infrastructure` as a resource,
  so the single-file release carries it and it is never read from a folder the
  user can edit. `catalog/README.md` holds the maintenance rule and the steps
  to add an entry.
- The catalog is loaded through the `IKnownItemsCatalogSource` port and checked
  by `KnownItemsCatalog.Validate` in Core: unique ids, a `https` source and a
  license on every entry, at least one fingerprint each, and conflicts that
  name two existing entries. A test loads the shipped catalog, so a bad entry
  fails CI rather than a user's diagnostics.
- Each fingerprint kind (`AddInUid`, `OverrideFile`, `GameFile`,
  `LargeAddressAware`) is matched by its own `IFingerprintMatcher`. This is the
  SDD §5 Strategy pattern: a new kind is a new class. The `DetectKnownItems` use
  case runs the matchers for every entry (Composite). `KnownItemsRule` turns
  the result into diagnostics.
- A byte marker in a `GameFile` fingerprint must be a whole NUL-terminated
  string in the file. The NUL-terminated string that follows it is the version.
- Matching only reads files. Nothing in the catalog is downloaded, installed or
  redistributed.

## Consequences
Adding a fix or mod is a data change plus a test. Adding a new way of
recognizing items is a new matcher. The catalog changes only with a release,
which is intended until an ADR allows fetching updates (SDD §12 forbids
network access before Phase 3). `scripts/catalog-fingerprint.ps1` prints the
UIDs and file lists a new entry needs from the mod's download.

## Alternatives considered
- *A C# class per item:* every catalog change would be a code change, and the data could not be reviewed as data.
- *A catalog file next to the executable:* users could edit it by accident, and the single-file release would need an extra file.
- *Hash fingerprints only:* every release of every mod would need its own entry, and DXVK's version could not be reported.
