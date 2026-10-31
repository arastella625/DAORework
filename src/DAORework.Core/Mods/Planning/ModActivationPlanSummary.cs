namespace DAORework.Core.Mods.Planning;

/// <summary>
/// A <see cref="ModActivationPlan"/> in one sentence, for the window's plans (P2.5-18): what it does to the files and
/// <c>AddIns.xml</c>, and how much it writes. The file-by-file detail stays in <see cref="ModActivationPlanText"/>.
/// </summary>
public static class ModActivationPlanSummary
{
    public static string Line(ModActivationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (plan.IsBlocked)
        {
            return "Blocked: nothing changes until the problems below are resolved.";
        }

        if (plan.IsEmpty)
        {
            return "Nothing to change: the folder already matches.";
        }

        var parts = new List<string>();
        Add(parts, plan.Count(PlanAction.Copy), "copies", files: true);
        Add(parts, plan.Count(PlanAction.Replace), "replaces", files: parts.Count == 0);
        Add(parts, plan.Count(PlanAction.Move), "moves", files: parts.Count == 0);
        Add(parts, plan.Count(PlanAction.Delete), "deletes", files: parts.Count == 0);
        Add(parts, plan.AdoptedInPlaceCount, "takes over", files: parts.Count == 0, " where they are");
        var added = plan.AddInsChanges.Count(c => c.Add);
        var removed = plan.AddInsChanges.Count - added;
        Add(parts, added, "adds", files: false, added == 1 ? " add-in to AddIns.xml" : " add-ins to AddIns.xml");
        Add(parts, removed, "removes", files: false, removed == 1 ? " add-in from AddIns.xml" : " add-ins from AddIns.xml");

        var sentence = Capitalize(Join(parts)) + ".";
        if (plan.BytesToWrite.Bytes > 0)
        {
            sentence += $" Writes {plan.BytesToWrite}.";
        }

        return sentence;
    }

    private static void Add(List<string> parts, int count, string verb, bool files, string suffix = "")
    {
        if (count > 0)
        {
            parts.Add(files ? $"{verb} {ModActivationPlanWarnings.Files(count)}{suffix}" : $"{verb} {count}{suffix}");
        }
    }

    private static string Join(List<string> parts) => parts.Count switch
    {
        1 => parts[0],
        _ => $"{string.Join(", ", parts.Take(parts.Count - 1))} and {parts[^1]}",
    };

    private static string Capitalize(string text) => char.ToUpperInvariant(text[0]) + text[1..];
}
