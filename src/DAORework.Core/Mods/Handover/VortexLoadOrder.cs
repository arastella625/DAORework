namespace DAORework.Core.Mods.Handover;

/// <summary>
/// Arranges the copies so the same files win as in Vortex (ADR-0028 §3): where several mods have a file, the record's
/// <c>source</c> is the copy the game uses, so that mod comes after the others, since the last mod wins. Mods with no
/// such tie keep their given order. Orders that contradict each other can't all hold, so those mods keep theirs and are
/// named. Pure.
/// </summary>
public static class VortexLoadOrder
{
    private static readonly StringComparer _names = StringComparer.OrdinalIgnoreCase;

    /// <param name="mods">Each mod with the paths of all its files, below the folder Vortex deploys to.</param>
    /// <param name="winners">For each placed file's path, the mod whose copy the game uses.</param>
    public static (IReadOnlyList<string> Order, IReadOnlyList<string> Unsettled) Arrange(
        IReadOnlyList<(string Mod, IReadOnlyList<string> Files)> mods,
        IReadOnlyDictionary<string, string> winners)
    {
        ArgumentNullException.ThrowIfNull(mods);
        ArgumentNullException.ThrowIfNull(winners);
        var mustFollow = mods.ToDictionary(m => m.Mod, _ => new HashSet<string>(_names), _names);
        foreach (var (mod, files) in mods)
        {
            foreach (var file in files)
            {
                if (winners.TryGetValue(file, out var winner) && !_names.Equals(winner, mod) && mustFollow.TryGetValue(winner, out var earlier))
                {
                    earlier.Add(mod);
                }
            }
        }

        var order = new List<string>();
        var left = mods.Select(m => m.Mod).ToList();
        while (left.FirstOrDefault(m => !mustFollow[m].Overlaps(left)) is { } next)
        {
            order.Add(next);
            left.Remove(next);
        }

        order.AddRange(left);
        return (order, left);
    }
}
