using DAORework.Core.Mods.Conflicts;
using DAORework.Core.Mods.Planning;

namespace DAORework.App.ViewModels;

/// <summary>
/// One override file that more than one active mod ships (P2.5-05, the P2-02 scanner): which mods ship it and whose copy
/// the game gets. The mod later in the load order wins.
/// </summary>
public sealed record FileConflictRow(string FileName, string Winner, string Detail)
{
    private const int FilesNamed = 3;

    /// <summary>Gets a value indicating whether every copy is the same, so the order changes nothing for this file.</summary>
    public bool SameContent { get; init; }

    /// <summary>Many files the same mods share, as one row (P2.6-16).</summary>
    public static FileConflictRow From(SharedFileGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        var more = group.FileNames.Count - FilesNamed;
        var names = string.Join(", ", group.FileNames.Take(FilesNamed));
        return new FileConflictRow(group.Title, group.Kinds, more > 0 ? $"{names} and {more} more." : $"{names}.") { SameContent = group.SameContent };
    }

    /// <param name="conflict">The conflict the scanner found.</param>
    /// <param name="names">Mod names by id, for the mods in the load order.</param>
    public static FileConflictRow From(OverrideConflict conflict, IReadOnlyDictionary<string, string> names)
    {
        ArgumentNullException.ThrowIfNull(conflict);
        ArgumentNullException.ThrowIfNull(names);
        string Name(string id) => names.TryGetValue(id, out var name) ? name : id;
        var losers = string.Join(", ", conflict.OtherModIds.Select(Name));
        var detail = conflict.SameContent
            ? $"Also in {losers}, with the same content, so the order changes nothing for it."
            : $"Also in {losers}, whose copy is not used.{(SharedFileMeaning.For(conflict.FileName) is { } meaning ? " " + meaning : string.Empty)}";
        return new FileConflictRow(conflict.FileName, $"{Name(conflict.Winner.ModId)} wins", detail) { SameContent = conflict.SameContent };
    }
}
