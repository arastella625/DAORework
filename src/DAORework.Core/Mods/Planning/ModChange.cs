namespace DAORework.Core.Mods.Planning;

/// <summary>
/// What the user asked for, turned into the ordered list of mods to have active. Activating appends mods not yet active
/// (so they take priority over earlier ones); deactivating removes them; setting replaces the list outright.
/// </summary>
public sealed record ModChange(ModChangeKind Kind, IReadOnlyList<string> ModIds)
{
    public IReadOnlyList<string> Apply(IReadOnlyList<string> active)
    {
        ArgumentNullException.ThrowIfNull(active);
        var comparer = StringComparer.OrdinalIgnoreCase;
        return Kind switch
        {
            ModChangeKind.Activate => active.Concat(ModIds.Where(id => !active.Contains(id, comparer))).Distinct(comparer).ToList(),
            ModChangeKind.Deactivate => active.Where(id => !ModIds.Contains(id, comparer)).ToList(),
            _ => ModIds.Distinct(comparer).ToList(),
        };
    }
}
