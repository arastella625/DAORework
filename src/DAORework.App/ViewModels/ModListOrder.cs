namespace DAORework.App.ViewModels;

/// <summary>Search and sort for the Mods tab's list (P2.6-30).</summary>
public static class ModListOrder
{
    /// <summary>Whether <paramref name="title"/> holds what was typed, in any letter case. Nothing typed matches everything.</summary>
    public static bool Matches(string title, string? search)
    {
        ArgumentNullException.ThrowIfNull(title);
        return string.IsNullOrWhiteSpace(search) || title.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The rows in the order asked for; rows that tie stay as they were.</summary>
    public static IEnumerable<LibraryModRow> Sorted(IEnumerable<LibraryModRow> rows, ModSort sort)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return sort switch
        {
            ModSort.Newest => rows.OrderByDescending(r => r.AddedAt),
            ModSort.OnFirst => rows.OrderByDescending(r => r.IsActive),
            _ => rows,
        };
    }
}
