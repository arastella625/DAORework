namespace DAORework.Core.Mods.Troubleshooting;

/// <summary>The games a documents folder was last played with, oldest first (ADR-0043). Pure.</summary>
public sealed record PlayHistory(IReadOnlyList<PlayRecord> Plays)
{
    public const int CurrentSchemaVersion = 1;

    private const int Kept = 10;

    private static readonly StringComparer _ids = StringComparer.OrdinalIgnoreCase;

    public static PlayHistory Empty { get; } = new([]);

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>This history with one more game. The same mods again only move the time on.</summary>
    public PlayHistory With(PlayRecord play)
    {
        ArgumentNullException.ThrowIfNull(play);
        var earlier = Plays.Count > 0 && Plays[^1].ModIds.SequenceEqual(play.ModIds, _ids) ? Plays.SkipLast(1) : Plays;
        return this with { Plays = [.. earlier.Append(play).TakeLast(Kept)] };
    }

    /// <summary>What differs from the latest game with other mods. The latest game with these mods says nothing, since it may be the broken one.</summary>
    /// <param name="now">What is on now, in its order.</param>
    /// <returns>The differences, or null when no game had other mods.</returns>
    public PlayChanges? ChangesSince(IReadOnlyList<string> now)
    {
        ArgumentNullException.ThrowIfNull(now);
        if (Plays.LastOrDefault(p => !p.ModIds.SequenceEqual(now, _ids)) is not { } then)
        {
            return null;
        }

        var added = now.Except(then.ModIds, _ids).ToList();
        var removed = then.ModIds.Except(now, _ids).ToList();
        var both = now.Except(added, _ids);
        return new PlayChanges(then, added, removed, !both.SequenceEqual(then.ModIds.Except(removed, _ids), _ids));
    }
}
