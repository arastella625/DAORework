using CommunityToolkit.Mvvm.ComponentModel;
using DAORework.Core.Mods.Library;

namespace DAORework.App.ViewModels;

/// <summary>One mod's copies in the <i>Keep one of each</i> preview, and which is kept (ADR-0039).</summary>
public sealed partial class KeepEachRow : ObservableObject
{
    private readonly Action? _picked;
    private KeepChoice? _pick;

    /// <param name="set">The set as planned.</param>
    /// <param name="picked">Runs when the player changes the pick.</param>
    public KeepEachRow(KeepEachSet set, Action picked)
    {
        ArgumentNullException.ThrowIfNull(set);
        Group = set.Group;
        Title = ModDisplayName.For(set.Copies[0].Mod.Name);
        Choices = KeepEachWords.Choices(set);
        Pick = Choices.First(c => set.KeepsAll ? c.ModId is null : c.ModId == set.Plan.Kept.Mod.Id);
        Update(set);
        _picked = picked;
    }

    public string Group { get; }

    public string Title { get; }

    public IReadOnlyList<KeepChoice> Choices { get; }

    /// <summary>Gets or sets what is kept. A list being taken away sends null, which isn't a choice.</summary>
    public KeepChoice? Pick
    {
        get => _pick;
        set
        {
            if (value is not null && SetProperty(ref _pick, value))
            {
                _picked?.Invoke();
            }
        }
    }

    /// <summary>Gets what the pick means for this set, when there's something to say.</summary>
    [ObservableProperty]
    public partial string? Note { get; private set; }

    /// <summary>Gets why this set can't be cleared, and what to do.</summary>
    [ObservableProperty]
    public partial string? Stopped { get; private set; }

    /// <summary>Takes in a new plan, which leaves the pick and the row's place as they are.</summary>
    public void Update(KeepEachSet set)
    {
        ArgumentNullException.ThrowIfNull(set);
        Note = KeepEachWords.Note(set);
        Stopped = KeepEachWords.Stopped(set);
    }
}
