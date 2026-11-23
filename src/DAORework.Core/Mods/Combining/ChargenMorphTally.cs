namespace DAORework.Core.Mods.Combining;

/// <summary>
/// What each library mod's <c>chargenmorphcfg.xml</c> adds, against the game's own copy where it could be read, else
/// against what every mod's copy shares (ADR-0025 amended, ADR-0050 §4). With one such mod and no game copy there is
/// nothing to count against. Pure.
/// </summary>
public sealed class ChargenMorphTally
{
    private readonly IReadOnlyDictionary<string, ChargenMorphEntries> _copies;
    private readonly ChargenMorphEntries? _baseline;

    /// <param name="copies">Each mod's readable copy, by mod id.</param>
    /// <param name="game">The game's own copy, or null when it couldn't be read.</param>
    public ChargenMorphTally(IReadOnlyDictionary<string, ChargenMorphEntries> copies, ChargenMorphEntries? game)
    {
        ArgumentNullException.ThrowIfNull(copies);
        _copies = copies;
        _baseline = game ?? (copies.Count >= 2 ? ChargenMorphEntries.Shared([.. copies.Values]) : null);
        AgainstTheGame = game is not null;
    }

    /// <summary>Gets a tally of no mods, before anything is read.</summary>
    public static ChargenMorphTally Empty { get; } = new(new Dictionary<string, ChargenMorphEntries>(), null);

    /// <summary>Gets a value indicating whether the game's own copy is what mods are counted against.</summary>
    public bool AgainstTheGame { get; }

    /// <summary>What one mod adds, or null when it has no readable copy.</summary>
    public ChargenMorphAdds? AddsOf(string modId)
    {
        if (!_copies.TryGetValue(modId, out var copy))
        {
            return null;
        }

        if (_baseline is null)
        {
            return ChargenMorphAdds.Uncounted;
        }

        // Against what the copies share, a copy two mods both ship adds nothing in counts, though it adds to the game.
        var beyond = copy.Beyond(_baseline);
        return beyond.Count == 0 && !AgainstTheGame ? ChargenMorphAdds.Uncounted : new ChargenMorphAdds(beyond, Counted: true);
    }

    /// <summary>What the character creator gained going from the mods in <paramref name="before"/> to those in <paramref name="after"/>.</summary>
    public ChargenMorphAdds? Gained(IEnumerable<string> before, IEnumerable<string> after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        var now = Copies(after);
        if (now.Count == 0)
        {
            return null;
        }

        if (_baseline is null)
        {
            return ChargenMorphAdds.Uncounted;
        }

        var had = ChargenMorphEntries.Union([_baseline, .. Copies(before)]);
        return new ChargenMorphAdds(ChargenMorphEntries.Union(now).Beyond(had), Counted: true);
    }

    private List<ChargenMorphEntries> Copies(IEnumerable<string> modIds) =>
        [.. modIds.Distinct(StringComparer.OrdinalIgnoreCase).Select(id => _copies.GetValueOrDefault(id)).OfType<ChargenMorphEntries>()];
}
