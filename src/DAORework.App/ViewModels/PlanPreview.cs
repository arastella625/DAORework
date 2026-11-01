using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DAORework.Core.Mods.Planning;

namespace DAORework.App.ViewModels;

/// <summary>
/// A mod plan as the window shows it (P2.5-18): a title, one sentence saying what it does, its warnings and blockers
/// in full, and the file-by-file detail behind <i>Show details</i>. Mods are named by their names, from
/// <see cref="NameOf"/>; the command line keeps their ids.
/// </summary>
public sealed partial class PlanPreview : ObservableObject
{
    /// <summary>Gets or sets how a mod id is shown; an id it does not know is shown as it is.</summary>
    public Func<string, string> NameOf { get; set; } = id => id;

    /// <summary>Gets the warnings and blockers, always shown.</summary>
    public ObservableCollection<string> Notes { get; } = [];

    /// <summary>Gets the plan's full text, shown on <i>Show details</i>.</summary>
    public ObservableCollection<string> Lines { get; } = [];

    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    public partial string? Summary { get; set; }

    [ObservableProperty]
    public partial bool ShowDetails { get; set; }

    /// <param name="title">What the plan is for, such as "Activate Eyes".</param>
    /// <param name="plan">The plan.</param>
    /// <param name="summary">A sentence to show instead of the plan's own, such as why an order changes nothing.</param>
    public void Show(string? title, ModActivationPlan plan, string? summary = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        Clear();
        Title = title;
        Summary = summary ?? ModActivationPlanSummary.Line(plan, NameOf);
        foreach (var warning in ModActivationPlanWarnings.For(plan, NameOf))
        {
            Notes.Add(warning);
        }

        foreach (var blocker in plan.Blockers)
        {
            Notes.Add($"Cannot apply: {blocker.Message}");
        }

        foreach (var line in ModActivationPlanText.Render(plan, nameOf: NameOf))
        {
            Lines.Add(line);
        }
    }

    public void Clear()
    {
        Title = null;
        Summary = null;
        ShowDetails = false;
        Notes.Clear();
        Lines.Clear();
    }
}
