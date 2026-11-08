using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DAORework.Core.Mods.Planning;

namespace DAORework.App.ViewModels;

/// <summary>
/// A mod plan as the window shows it (P2.5-18): a title, one sentence saying what it does, what stops it and what to do,
/// a few warnings however big the plan is, and every file behind <i>Show details</i>. Mods are named by their names,
/// from <see cref="NameOf"/>; the command line keeps their ids.
/// </summary>
public sealed partial class PlanPreview : ObservableObject
{
    /// <summary>Gets or sets how a mod id is shown; an id it does not know is shown as it is.</summary>
    public Func<string, string> NameOf { get; set; } = id => id;

    /// <summary>Gets what stops the plan and what to do about it, shown first.</summary>
    public ObservableCollection<PlanProblem> Problems { get; } = [];

    /// <summary>Gets the warnings, always shown, and grouped once a plan has many.</summary>
    public ObservableCollection<string> Notes { get; } = [];

    /// <summary>Gets the plan's full text, shown on <i>Show details</i>.</summary>
    public ObservableCollection<string> Lines { get; } = [];

    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    public partial string? Summary { get; set; }

    [ObservableProperty]
    public partial bool ShowDetails { get; set; }

    /// <summary>A preview's title: the mods by name while that fits on a line, then how many. <i>Show details</i> names them all.</summary>
    public static string TitleFor(string verb, IReadOnlyList<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);
        var list = string.Join(", ", names);
        return names.Count switch
        {
            1 => $"{verb} {list}",
            <= PlanWords.NamesShown => $"{verb} {names.Count} mods: {list}",
            _ => $"{verb} {names.Count} mods",
        };
    }

    /// <param name="title">What the plan is for, such as "Activate Eyes".</param>
    /// <param name="plan">The plan.</param>
    /// <param name="summary">A sentence to show instead of the plan's own, such as why an order changes nothing.</param>
    public void Show(string? title, ModActivationPlan plan, string? summary = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        Clear();
        Title = title;
        Summary = summary ?? ModActivationPlanSummary.Line(plan, NameOf);
        foreach (var problem in PlanProblems.For(plan, NameOf))
        {
            Problems.Add(problem);
        }

        foreach (var note in PlanNotes.For(plan, NameOf))
        {
            Notes.Add(note);
        }

        var text = ModActivationPlanText.Render(plan, nameOf: NameOf);
        var changes = ModActivationPlanText.Changes(plan, ModActivationPlanWarnings.CombinedAware(NameOf));
        foreach (var line in changes.Count > 0 ? [.. changes, .. text.Skip(1)] : text)
        {
            Lines.Add(line);
        }
    }

    public void Clear()
    {
        Title = null;
        Summary = null;
        ShowDetails = false;
        Problems.Clear();
        Notes.Clear();
        Lines.Clear();
    }
}
