using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;

namespace DAORework.Core.Mods.Conflicts;

/// <summary>
/// Sorts files mods share by what can go wrong (P2.6-54), so a long equal list becomes what matters: <i>3 could break
/// quests, 47 only change how things look</i>. It goes by the kind of file, so it says <i>could</i>. Pure.
/// </summary>
public static class SharedFileRank
{
    private static readonly HashSet<string> _quests = new(StringComparer.OrdinalIgnoreCase)
    {
        ".ncs", ".gda", ".dlg", ".cnv", ".plo", ".are", ".utc", ".utp",
    };

    public static SharedFileImpact Of(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return _quests.Contains(extension) ? SharedFileImpact.CouldBreakQuests
            : GameDataFiles.Looks.Contains(extension) ? SharedFileImpact.Looks
            : SharedFileImpact.Other;
    }

    /// <returns>A sentence counting each impact, or null when there are no files.</returns>
    public static string? Summary(IReadOnlyList<string> fileNames)
    {
        ArgumentNullException.ThrowIfNull(fileNames);
        var counts = fileNames.GroupBy(Of).ToDictionary(g => g.Key, g => g.Count());
        var parts = new[]
        {
            Part(counts, SharedFileImpact.CouldBreakQuests, "could break quests", "could break quests"),
            Part(counts, SharedFileImpact.Looks, "only changes how things look", "only change how things look"),
            Part(counts, SharedFileImpact.Other, "is something else", "are something else"),
        }.OfType<string>().ToList();
        return parts.Count == 0 ? null : PlanWords.List(parts) + ".";
    }

    private static string? Part(Dictionary<SharedFileImpact, int> counts, SharedFileImpact impact, string one, string many) =>
        counts.TryGetValue(impact, out var count) ? $"{count} {(count == 1 ? one : many)}" : null;
}
