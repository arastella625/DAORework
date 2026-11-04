using System.Globalization;

namespace DAORework.Core.Mods.Planning;

/// <summary>How the window's plan sentences write a list and a count (VOICE.md: as a person writes them).</summary>
internal static class PlanWords
{
    private const int NamesShown = 3;

    /// <summary>"a", "a and b", "a, b and c".</summary>
    public static string List(IReadOnlyList<string> parts) => parts.Count switch
    {
        0 => string.Empty,
        1 => parts[0],
        _ => $"{string.Join(", ", parts.Take(parts.Count - 1))} and {parts[^1]}",
    };

    /// <summary>The mods by name, the first three of a longer list, then how many more.</summary>
    public static string Names(IEnumerable<string> modIds, Func<string, string> nameOf)
    {
        var names = modIds.Distinct(StringComparer.OrdinalIgnoreCase).Select(nameOf).ToList();
        return names.Count <= NamesShown
            ? List(names)
            : $"{string.Join(", ", names.Take(NamesShown))} and {Count(names.Count - NamesShown, "more", "more")}";
    }

    public static string Count(int count, string one, string many) =>
        string.Create(CultureInfo.InvariantCulture, $"{count} {(count == 1 ? one : many)}");
}
