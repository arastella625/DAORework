# ERF

An archive of game files, such as the `.erf` files in `packages/core/data` and in mods. DAORework reads versions 2.0 and
2.2, the ones Dragon Age: Origins uses. V3.0 is Dragon Age II's.

**Source:** the [Dragon Age Toolset wiki's ERF page](http://www.datoolset.net/wiki/ERF), a public community specification.
Read on 2026-11-04. No code was ported from anywhere.

All numbers are little-endian. Names are UTF-16, and a name shorter than its field ends at a NUL.

## V2.0

| Field | Size | Notes |
|---|---|---|
| magic | 16 bytes | `ERF V2.0` in UTF-16 |
| fileCount | 4 | |
| year | 4 | Years since 1900 |
| day | 4 | Days since 1 January |
| unknown | 4 | Possibly always `0xFFFFFFFF` |

Then `fileCount` entries:

| Field | Size | Notes |
|---|---|---|
| name | 64 bytes | 32 UTF-16 characters |
| offset | 4 | From the start of the file |
| size | 4 | |

## V2.2

The same header, followed by:

| Field | Size | Notes |
|---|---|---|
| flags | 4 | Bits 4 to 7 are the encryption scheme, bits 29 to 31 the compression |
| moduleId | 4 | Finds the password when a file is encrypted |
| passwordDigest | 16 | MD5 of the password |

Each entry adds one field: `size` becomes `packedSize`, followed by `unpackedSize`.

## What DAORework does with it
`ErfReader` reads the table of contents only, and never a file's data, so encryption and compression don't matter to it.
A table that can't fit in the file, or an entry pointing past its end, is refused.
