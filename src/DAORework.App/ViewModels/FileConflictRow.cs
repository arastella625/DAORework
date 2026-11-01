using DAORework.Core.Mods.Conflicts;

namespace DAORework.App.ViewModels;

/// <summary>
/// One override file that more than one active mod ships (P2.5-05, the P2-02 scanner): which mods ship it and whose copy
/// the game gets. The mod later in the load order wins.
/// </summary>
public sealed record FileConflictRow(string FileName, string Winner, string Detail)
{
    /// <param name="conflict">The conflict the scanner found.</param>
    /// <param name="names">Mod names by id, for the mods in the load order.</param>
    public static FileConflictRow From(OverrideConflict conflict, IReadOnlyDictionary<string, string> names)
    {
        ArgumentNullException.ThrowIfNull(conflict);
        ArgumentNullException.ThrowIfNull(names);
        string Name(string id) => names.TryGetValue(id, out var name) ? name : id;
        var losers = string.Join(", ", conflict.Overridden.Select(f => Name(f.ModId)).Distinct(StringComparer.Ordinal));
        var detail = conflict.SameContent
            ? $"Also in {losers}, with the same content, so the order changes nothing for it."
            : $"Also in {losers}, whose copy is not used.{(SharedFileMeaning.For(conflict.FileName) is { } meaning ? " " + meaning : string.Empty)}";
        return new FileConflictRow(conflict.FileName, $"{Name(conflict.Winner.ModId)} wins", detail);
    }
}
