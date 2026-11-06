using DAORework.Core.Common;
using DAORework.Core.Mods.Library;

namespace DAORework.App.ViewModels;

/// <summary>What the <i>Keep this one</i> preview says about what can't come back (ADR-0039).</summary>
internal static class KeepCopyWords
{
    private const int NamesShown = 5;

    /// <summary>What goes, by name when there's one and its name tells it from the kept copy.</summary>
    public static string Removing(KeepCopyPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return plan.Removing switch
        {
            [var only] when only.Mod.Name != plan.Kept.Mod.Name => ModDisplayName.For(only.Mod.Name),
            [_] => "the other copy",
            _ => $"{plan.Removing.Count} other copies",
        };
    }

    /// <summary>One copy being removed, for <i>Show details</i>: where it came from tells copies of one name apart.</summary>
    public static string Line(LibraryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return $"Removes {ModDisplayName.For(entry.Mod.Name)}, {new ByteSize(entry.SizeBytes)}, added from {entry.SourceName}";
    }

    public static string Permanent(bool swapped) => swapped
        ? "Removing can't be undone, and History can't go back past this until you add the copy again."
        : "Removing can't be undone. You can add the mod again from its download.";

    /// <summary>What goes for good, or null when the kept copy holds it all.</summary>
    public static string? Lost(IReadOnlyList<string> onlyInRemoved)
    {
        if (onlyInRemoved.Count == 0)
        {
            return null;
        }

        var names = string.Join(", ", onlyInRemoved.Take(NamesShown));
        var more = onlyInRemoved.Count > NamesShown ? $" and {onlyInRemoved.Count - NamesShown} more" : string.Empty;
        return $"These copies aren't the same. Only the ones being removed have {names}{more}.";
    }
}
