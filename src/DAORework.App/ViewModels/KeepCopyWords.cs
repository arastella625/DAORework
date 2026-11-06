using DAORework.Core.Common;
using DAORework.Core.Mods.Library;

namespace DAORework.App.ViewModels;

/// <summary>What the <i>Keep this one</i> preview says about what can't come back (ADR-0039).</summary>
internal static class KeepCopyWords
{
    private const int NamesShown = 5;

    /// <summary>The warning, for any copy: a change it was part of can't be undone without it, whether or not it is on now.</summary>
    public static string Warning(bool recycled) => recycled
        ? "Removed copies go to the Recycle Bin. History can't go back to a time one was on until you put it back, or add the same download again."
        : "Removing can't be undone. History can't go back to a time a removed copy was on, unless you add the same download again.";

    /// <summary>What happens to the space: the Recycle Bin keeps it until it is emptied.</summary>
    public static string Space(long bytes, bool recycled) => recycled
        ? $"{new ByteSize(bytes)} goes to the Recycle Bin, and comes back when you empty it."
        : $"You get back {new ByteSize(bytes)}.";

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
