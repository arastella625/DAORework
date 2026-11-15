namespace DAORework.Core.Mods.AddIns;

/// <summary>Each add-in's mark on Mods (P2.5-23), from what <c>AddIns.xml</c> enables and the files they share. Pure.</summary>
public static class AddInMarks
{
    /// <summary>The most shared files a row's details name; the rest are counted.</summary>
    public const int SentencesShown = 5;

    /// <summary>Every enabled add-in by its UID, without case.</summary>
    public static IReadOnlyDictionary<string, AddInMark> For(AddInsReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var marks = new Dictionary<string, AddInMark>(StringComparer.OrdinalIgnoreCase);
        foreach (var addIn in report.LoadOrder.Where(a => !marks.ContainsKey(a.Uid)))
        {
            var shared = report.Clashes.Where(c => Same(c.Winner, addIn) || c.Others.Any(o => Same(o, addIn))).ToList();
            var wins = shared.Any(c => Same(c.Winner, addIn));
            var sentences = shared.Take(SentencesShown).Select(AddInClashWords.Sentence).ToList();
            if (shared.Count > SentencesShown)
            {
                var more = shared.Count - SentencesShown;
                sentences.Add(more == 1 ? "1 more file is in it and another add-in." : $"{more} more files are in it and another add-in.");
            }

            if (shared.Count > 0)
            {
                sentences.Add(AddInClashWords.Advice);
            }

            marks[addIn.Uid] = new AddInMark(addIn.Priority, wins, shared.Any(c => !Same(c.Winner, addIn)), sentences);
        }

        return marks;
    }

    private static bool Same(ListedAddIn a, ListedAddIn b) => string.Equals(a.Uid, b.Uid, StringComparison.OrdinalIgnoreCase);
}
