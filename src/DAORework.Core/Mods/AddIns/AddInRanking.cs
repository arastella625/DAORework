namespace DAORework.Core.Mods.AddIns;

/// <summary>
/// How the game ranks add-ins, as checked on the owner's install on 2026-11-15 (issue #72). It loads them from the
/// highest <c>Priority</c> to the lowest, and on equal priorities in reverse of their order in <c>AddIns.xml</c>. The
/// add-in loaded last wins a file they share: the lowest priority, or on a tie the entry listed first. Pure.
/// </summary>
public static class AddInRanking
{
    /// <summary>Puts add-ins in the order the game loads them, the winner of any shared file last.</summary>
    public static IReadOnlyList<ListedAddIn> LoadOrder(IEnumerable<ListedAddIn> addIns)
    {
        ArgumentNullException.ThrowIfNull(addIns);
        return [.. addIns.OrderByDescending(a => a.Priority).ThenByDescending(a => a.Position)];
    }

    /// <summary>Gets the add-in whose copy of a shared file the game uses.</summary>
    public static ListedAddIn Winner(IEnumerable<ListedAddIn> addIns)
    {
        ArgumentNullException.ThrowIfNull(addIns);
        return addIns.OrderBy(a => a.Priority).ThenBy(a => a.Position).First();
    }
}
