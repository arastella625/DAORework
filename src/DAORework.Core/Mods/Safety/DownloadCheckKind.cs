namespace DAORework.Core.Mods.Safety;

/// <summary>What the catalog says about a library mod's download (ADR-0018).</summary>
public enum DownloadCheckKind
{
    /// <summary>The catalog knows no download of this mod, so there is nothing to compare it with.</summary>
    Unknown,

    /// <summary>The download is byte for byte one the catalog records.</summary>
    Matches,

    /// <summary>The catalog recognizes the mod, but the download matches none of its known downloads.</summary>
    Differs,

    /// <summary>The catalog knows downloads of the mod, but it was imported from a folder, which has no download hash.</summary>
    FromAFolder,
}
