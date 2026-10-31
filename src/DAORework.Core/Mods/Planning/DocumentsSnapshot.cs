namespace DAORework.Core.Mods.Planning;

/// <summary>
/// The documents folder as the planner sees it: SHA-256 by documents-relative path for the paths asked about that exist,
/// the override files outside DAORework's own subfolder (relative to the override folder), and the registered add-in UIDs.
/// </summary>
public sealed record DocumentsSnapshot(
    IReadOnlyDictionary<string, string> Hashes,
    IReadOnlyList<string> UnmanagedOverridePaths,
    IReadOnlyList<string> RegisteredAddInUids);
