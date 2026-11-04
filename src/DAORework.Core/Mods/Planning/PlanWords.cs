using System.Globalization;

namespace DAORework.Core.Mods.Planning;

/// <summary>How the window's plan sentences write a list and a count (VOICE.md: as a person writes them).</summary>
public static class PlanWords
{
    /// <summary>How many names a sentence gives before it counts the rest.</summary>
    public const int NamesShown = 3;

    /// <summary>"a", "a and b", "a, b and c".</summary>
    public static string List(IReadOnlyList<string> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        return parts.Count < 2 ? string.Concat(parts) : $"{string.Join(", ", parts.Take(parts.Count - 1))} and {parts[^1]}";
    }

    /// <summary>The mods by name, the first three of a longer list, then how many more.</summary>
    public static string Names(IEnumerable<string> modIds, Func<string, string> nameOf)
    {
        ArgumentNullException.ThrowIfNull(modIds);
        ArgumentNullException.ThrowIfNull(nameOf);
        var names = modIds.Distinct(StringComparer.OrdinalIgnoreCase).Select(nameOf).ToList();
        return names.Count <= NamesShown
            ? List(names)
            : $"{string.Join(", ", names.Take(NamesShown))} and {names.Count - NamesShown} more";
    }

    public static string Count(int count, string one, string many) =>
        string.Create(CultureInfo.InvariantCulture, $"{count} {(count == 1 ? one : many)}");
}
