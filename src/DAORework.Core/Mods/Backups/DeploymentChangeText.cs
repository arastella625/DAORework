namespace DAORework.Core.Mods.Backups;

/// <summary>
/// What a backed-up change did, in a few words, from what was active before it and after it (P2.5-19), in the window's
/// words (docs/VOICE.md): <i>Turned on Eyes, Nails</i>, <i>Started managing 4 mods: …</i>, <i>Changed the order</i>. A backup records only what was active
/// before its change; what came after is the next backup's <i>before</i>, or what is active now. Pure.
/// </summary>
public static class DeploymentChangeText
{
    private const int NamesShown = 3;

    /// <summary>Gets the words for mod changes on the Mods tab.</summary>
    public static ChangeWords Mods { get; } = new("Turned on", "Turned off", "Started managing", "mods");

    /// <summary>Gets the words for fix changes on the Fixes tab.</summary>
    public static ChangeWords Fixes { get; } = new("Turned on", "Turned off", "Turned on", "fixes");

    /// <param name="before">What was active before the change, in order.</param>
    /// <param name="after">What was active after it, in order.</param>
    /// <param name="moved">Whether the change moved files into place, which only an adoption does (ADR-0015).</param>
    /// <param name="words">How this kind of change is worded.</param>
    /// <param name="nameOf">How an entry is named.</param>
    public static string Describe(IReadOnlyList<string> before, IReadOnlyList<string> after, bool moved, ChangeWords words, Func<string, string> nameOf)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(words);
        ArgumentNullException.ThrowIfNull(nameOf);
        var comparer = StringComparer.OrdinalIgnoreCase;
        var added = after.Where(id => !before.Contains(id, comparer)).ToList();
        var removed = before.Where(id => !after.Contains(id, comparer)).ToList();
        var parts = new List<string>();
        if (added.Count > 0)
        {
            parts.Add(Phrase(moved ? words.Moved : words.Added, added, words.Noun, nameOf));
        }

        if (removed.Count > 0)
        {
            var phrase = Phrase(words.Removed, removed, words.Noun, nameOf);
            parts.Add(parts.Count == 0 ? phrase : char.ToLowerInvariant(phrase[0]) + phrase[1..]);
        }

        if (parts.Count > 0)
        {
            return string.Join("; ", parts);
        }

        return before.SequenceEqual(after, comparer) ? $"Changed no {words.Noun}" : "Changed the order";
    }

    private static string Phrase(string verb, List<string> ids, string noun, Func<string, string> nameOf)
    {
        var names = ids.Select(nameOf).ToList();
        if (names.Count <= NamesShown)
        {
            return $"{verb} {Join(names)}";
        }

        return $"{verb} {names.Count} {noun}: {string.Join(", ", names.Take(NamesShown))} and {names.Count - NamesShown} more";
    }

    private static string Join(List<string> names) =>
        names.Count == 1 ? names[0] : $"{string.Join(", ", names.Take(names.Count - 1))} and {names[^1]}";
}
