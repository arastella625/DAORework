using DAORework.Core.Fixes;

namespace DAORework.Core.Catalog;

/// <summary>
/// What a mod needs that isn't in the game, worded for its preview (ADR-0033 §5). Only a warning: a player who sorted it
/// out some other way is never stopped. On Steam's protected executable the 4 GB patch can't go on (ADR-0023), so that
/// warning says so and stops there: it points to no replacement executable. Pure.
/// </summary>
public static class NeedsCheck
{
    /// <param name="catalog">The catalog, for the needed entries' names.</param>
    /// <param name="item">The catalog's entry for the mod.</param>
    /// <param name="modName">The mod's name in the Library.</param>
    /// <param name="missing">The ids of entries known not to be in the game. One that couldn't be checked is never in it.</param>
    /// <param name="steamProtected">Whether daorigins.exe is Steam's protected copy.</param>
    public static IReadOnlyList<string> Warnings(KnownItemsCatalog catalog, KnownItem item, string modName, IReadOnlySet<string> missing, bool steamProtected)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(missing);
        return [.. (item.Needs ?? [])
            .Where(need => missing.Contains(need.Item))
            .Select(need => Warning(modName, need.Item, catalog.Items.FirstOrDefault(i => i.Id.Equals(need.Item, StringComparison.OrdinalIgnoreCase)), steamProtected))];
    }

    /// <summary>Said instead when the game couldn't be checked: the preview still goes ahead, since a need is only a warning.</summary>
    public static string NotChecked(string modName, string reason) => $"Couldn't check what {modName} needs: {reason}";

    private static string Warning(string modName, string id, KnownItem? needed, bool steamProtected)
    {
        if (!id.Equals(FixIds.LargeAddressAware, StringComparison.OrdinalIgnoreCase))
        {
            var then = needed?.Kind == KnownItemKind.Fix ? "Apply it first on the Fixes tab." : "Turn it on first.";
            return $"{modName} needs {needed?.Name ?? id}, its author says, and it isn't in your game. {then}";
        }

        return steamProtected
            ? $"{modName} needs the 4 GB patch, its author says, and may crash without it. Your daorigins.exe is Steam's protected copy, which can't take the patch."
            : $"{modName} needs the 4 GB patch, its author says. Apply it first on the Fixes tab (fixes apply laa).";
    }
}
