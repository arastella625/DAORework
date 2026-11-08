namespace DAORework.Core.Mods.Troubleshooting;

/// <summary>
/// Whether the mods on now are the ones this round tests (P2.6-69). After an <i>Undo</i> in History the search still asked
/// about "the 31 still on" with all 62 back on, and an answer then would rule out the wrong mods. Pure.
/// </summary>
public static class ProblemSearchStep
{
    /// <param name="search">The search.</param>
    /// <param name="on">What is on now.</param>
    /// <returns>True when the round's mods are on, in any order, or the search has ended.</returns>
    public static bool InStep(ProblemSearch search, IReadOnlyCollection<string> on)
    {
        ArgumentNullException.ThrowIfNull(search);
        ArgumentNullException.ThrowIfNull(on);
        return search.End != ProblemSearchEnd.None || on.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(search.Keeping());
    }
}
