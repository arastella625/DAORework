using DAORework.Core.Mods.Library;

namespace DAORework.Core.Fixes;

/// <summary>
/// The managed fixes (ADR-0013), named as in the known-items catalog, and where their library entries live: a fix
/// entry's id is made like a mod's, and its folder is <c>library/fixes/{id}</c>.
/// </summary>
public static class FixIds
{
    public const string LargeAddressAware = "large-address-aware";
    public const string Dxvk = "dxvk";

    /// <summary>The folder under the library that holds fix entries; its name is never a mod id, which ends in a hash.</summary>
    public const string LibraryFolder = "fixes";

    public static string CreateEntryId(string fix, string? version, string sha256) =>
        ModIds.Create(version is null ? fix : $"{fix} {version}", sha256);

    /// <summary>The entry's folder relative to the library, as a deployment record and a plan name it.</summary>
    public static string LibraryId(string entryId) => $"{LibraryFolder}/{entryId}";

    /// <summary>Whether a record's library id is an entry of <paramref name="fix"/>: its id starts with the fix's name.</summary>
    public static bool IsEntryOf(string libraryId, string fix) =>
        libraryId.StartsWith($"{LibraryFolder}/{fix}-", StringComparison.OrdinalIgnoreCase);

    /// <summary>The name a person reads for a fix: "4 GB patch", "DXVK", or the id itself for one DAORework does not know.</summary>
    public static string DisplayName(string fix) => fix switch
    {
        LargeAddressAware => "4 GB patch",
        Dxvk => "DXVK",
        _ => fix,
    };
}
