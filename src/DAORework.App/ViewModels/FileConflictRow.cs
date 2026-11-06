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

    /// <summary>Gets what can go wrong here, which orders the rows (P2.6-54).</summary>
    public SharedFileImpact Impact { get; init; } = SharedFileImpact.Other;

    /// <summary>Gets the file as a player would say it, such as <i>Alistair's conversation</i>, when its name makes that plain.</summary>
    public string? PlainName { get; init; }

    /// <summary>Gets a value indicating whether this is worth a player's attention, so it shows in amber.</summary>
    public bool IsRisky => Impact == SharedFileImpact.CouldBreakQuests && !SameContent;

    /// <summary>Gets the row's tag.</summary>
    public string Badge => this switch
    {
        { SameContent: true } => "Same in each",
        { Impact: SharedFileImpact.CouldBreakQuests } => "Could break quests",
        { Impact: SharedFileImpact.Looks } => "Only looks",
        _ => "One wins",
    };

    /// <summary>Many files the same mods share, as one row (P2.6-16).</summary>
    public static FileConflictRow From(SharedFileGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        var more = group.FileNames.Count - FilesNamed;
        var names = string.Join(", ", group.FileNames.Take(FilesNamed));
        return new FileConflictRow(group.Title, group.Kinds, more > 0 ? $"{names} and {more} more." : $"{names}.")
        {
            SameContent = group.SameContent,
            Impact = group.FileNames.Min(SharedFileRank.Of),
        };
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
        return new FileConflictRow(conflict.FileName, $"{Name(conflict.Winner.ModId)} wins", detail)
        {
            SameContent = conflict.SameContent,
            Impact = SharedFileRank.Of(conflict.FileName),
            PlainName = SharedFileNames.For(conflict.FileName),
        };
    }

    /// <summary>A file inside archives, where the game's choice isn't known, with what a player can do about that.</summary>
    /// <param name="conflict">What the mods share.</param>
    /// <param name="mods">The mods, named.</param>
    public static FileConflictRow Packed(PackedConflict conflict, string mods)
    {
        ArgumentNullException.ThrowIfNull(conflict);
        var impact = SharedFileRank.Of(conflict.FileName);
        var next = impact switch
        {
            SharedFileImpact.Looks => "It only changes how things look, so you can leave it. If you don't like what you see, turn one of these mods off.",
            SharedFileImpact.CouldBreakQuests => "A quest could break. If something goes wrong there, turn one of these mods off to see if it stops, or look for a compatibility patch.",
            _ => "If something seems wrong, turn one of these mods off to see if it stops.",
        };
        return new FileConflictRow(conflict.FileName, "Inside an archive", $"In {mods}. The game uses one copy, and DAORework can't tell which. {next}")
        {
            Impact = impact,
            PlainName = SharedFileNames.For(conflict.FileName),
        };
    }
}
