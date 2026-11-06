namespace DAORework.Core.Mods.Library;

/// <summary>Where a mod stands among its copies: <see cref="Rank"/> 0 is the newest of its <see cref="Group"/>.</summary>
public sealed record ModCopy(string Group, int Rank, int Count)
{
    /// <summary>Gets another copy's name when it isn't this one's, since a player can't otherwise tell what this is a copy of.</summary>
    public string? AlsoNamed { get; init; }
}
