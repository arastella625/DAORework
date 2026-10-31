namespace DAORework.Core.Catalog;

/// <summary>How a <see cref="KnownItemFingerprint"/> is looked for on disk; each kind has its own matcher (ADR-0011).</summary>
public enum FingerprintKind
{
    /// <summary>An add-in UID registered in <c>Settings\AddIns.xml</c> (how a <c>.dazip</c> is installed).</summary>
    AddInUid,

    /// <summary>A file name anywhere under <c>packages\core\override</c>.</summary>
    OverrideFile,

    /// <summary>A path relative to the install directory, optionally containing <see cref="KnownItemFingerprint.Marker"/>.</summary>
    GameFile,

    /// <summary>The large-address-aware flag in the game executable's header.</summary>
    LargeAddressAware,

    /// <summary>
    /// The SHA-256 of an official download of the item, of <see cref="KnownItemFingerprint.Version"/>. Compared with a
    /// download at import (ADR-0018), never looked for on disk.
    /// </summary>
    DownloadSha256,
}
