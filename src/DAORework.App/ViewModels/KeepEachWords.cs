using DAORework.Core.Common;
using DAORework.Core.Mods.Library;

namespace DAORework.App.ViewModels;

/// <summary>What the <i>Keep one of each</i> preview says (ADR-0039).</summary>
internal static class KeepEachWords
{
    public const string Title = "Keep one of each";

    /// <summary>The warning, once for the whole preview.</summary>
    public const string Permanent = "Removing can't be undone. History can't go back to a time a removed copy was on, unless you add the same download again.";

    public static string Summary(KeepEachPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return plan.Clearing.Count == 0
            ? "Nothing is removed, since every copy stays."
            : $"Removes {Copies(plan.Removing.Count)} of {Mods(plan.Clearing.Count)} from your Library, and gets back {new ByteSize(plan.BytesFreed)}.";
    }

    public static string Button(KeepEachPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return plan.Clearing.Count == 0 ? "Remove copies" : $"Remove {Copies(plan.Removing.Count)}";
    }

    public static string Done(KeepEachPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return $"Kept one copy of {Mods(plan.Clearing.Count)}, and got back {new ByteSize(plan.BytesFreed)}.";
    }

    /// <summary>The notes above the list: what comes first and why, what can't be cleared, and the warning when anything goes.</summary>
    public static IEnumerable<string> Notes(KeepEachPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var differ = plan.Sets.Count(s => s.Differs);
        if (differ > 0)
        {
            yield return differ == 1 ? "1 mod has copies that aren't the same, so it comes first." : $"{differ} mods have copies that aren't the same, so they come first.";
        }

        var stuck = plan.Sets.Count(s => !s.KeepsAll && !s.Clears);
        if (stuck > 0)
        {
            yield return stuck == 1 ? "1 mod stays as it is, and says why." : $"{stuck} mods stay as they are, and say why.";
        }

        if (plan.Clearing.Count > 0)
        {
            yield return Permanent;
        }
    }

    /// <summary>What a set can keep: each copy, newest first, then all of them.</summary>
    public static List<KeepChoice> Choices(KeepEachSet set)
    {
        ArgumentNullException.ThrowIfNull(set);
        var choices = set.Copies.Select((e, rank) => new KeepChoice(Label(set, e, rank == 0), e.Mod.Id)).ToList();
        choices.Add(new KeepChoice("Keep all of them", null));
        return choices;
    }

    /// <summary>What a set's pick means, or null when there's nothing to say: its copies are the same and it changes nothing in the game.</summary>
    public static string? Note(KeepEachSet set)
    {
        ArgumentNullException.ThrowIfNull(set);
        if (!set.Clears)
        {
            return null;
        }

        var plan = set.Plan;
        var lost = KeepCopyWords.Lost(plan.OnlyInRemoved) ?? (set.Differs ? "These copies aren't the same, but this one has everything the others do." : null);
        var swapped = plan.SwappedOut is { } name && !set.On.Contains(plan.Kept.Mod.Id, StringComparer.OrdinalIgnoreCase)
            ? $"{ModDisplayName.For(name)} is on, so this one takes its place."
            : null;
        return lost is null && swapped is null ? null : string.Join(' ', new[] { lost, swapped }.OfType<string>());
    }

    /// <summary>Why a set can't be cleared and what to do, or null when it can.</summary>
    public static string? Stopped(KeepEachSet set)
    {
        ArgumentNullException.ThrowIfNull(set);
        return set.KeepsAll || set.Plan.Problems.Count == 0
            ? null
            : string.Join(' ', set.Plan.Problems.Select(p => $"{p.Title} {p.WhatToDo}"));
    }

    /// <summary>A copy by what tells it from the others: its name, or under the row's own name, the download it came from.</summary>
    private static string Label(KeepEachSet set, LibraryEntry entry, bool newest)
    {
        var name = entry.Mod.Name == set.Copies[0].Mod.Name ? entry.SourceName : ModDisplayName.For(entry.Mod.Name);
        var on = set.On.Contains(entry.Mod.Id, StringComparer.OrdinalIgnoreCase);
        return string.Join(", ", new[] { name, on ? "on" : null, newest ? "newest" : null }.OfType<string>());
    }

    private static string Copies(int count) => count == 1 ? "1 copy" : $"{count} copies";

    private static string Mods(int count) => count == 1 ? "1 mod" : $"{count} mods";
}
