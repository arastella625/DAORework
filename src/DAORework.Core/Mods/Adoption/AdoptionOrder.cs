namespace DAORework.Core.Mods.Adoption;

/// <summary>
/// The order mods are adopted in decides who wins a file name they share: the later one (ADR-0015). Vortex's record
/// doesn't say, since it only settles files at one path, so the player chooses (P2.6-21). Pure.
/// </summary>
public static class AdoptionOrder
{
    private static readonly StringComparer _ids = StringComparer.OrdinalIgnoreCase;

    /// <summary>Whether <paramref name="winner"/> can be made to lose: it and a mod it beats are both being adopted, since mods already on come first.</summary>
    public static bool CanSwap(IReadOnlyList<string> order, string winner, IReadOnlyList<string> others)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(others);
        return order.Contains(winner, _ids) && others.Any(o => order.Contains(o, _ids));
    }

    /// <summary>The order with <paramref name="winner"/> moved in front of the first of <paramref name="others"/>, so it loses to them.</summary>
    public static IReadOnlyList<string> Swap(IReadOnlyList<string> order, string winner, IReadOnlyList<string> others)
    {
        if (!CanSwap(order, winner, others))
        {
            return order;
        }

        var swapped = order.Where(id => !_ids.Equals(id, winner)).ToList();
        swapped.Insert(swapped.FindIndex(id => others.Contains(id, _ids)), order.First(id => _ids.Equals(id, winner)));
        return swapped;
    }
}
