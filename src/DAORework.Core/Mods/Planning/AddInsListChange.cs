namespace DAORework.Core.Mods.Planning;

/// <summary>An <c>AddInItem</c> the plan adds to or removes from <c>Settings\AddIns.xml</c>; nothing else in the file changes.</summary>
public sealed record AddInsListChange(string Uid, string ModId, bool Add)
{
    /// <summary>
    /// Gets the entry's XML when it is put back as it was, not taken from a mod's manifest (ADR-0028 §3). Such a change
    /// is official DLC: <see cref="ModId"/> holds its name, and the deployment record never counts it as DAORework's.
    /// </summary>
    public string? Item { get; init; }
}
