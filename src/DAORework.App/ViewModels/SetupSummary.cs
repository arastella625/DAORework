using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DAORework.App.ViewModels;

/// <summary>
/// Home's readiness strip (ADR-0026): add-ins, override mods and combined files, each with what is on and whether it is
/// ready. Worked out from the Mods tab's own lists as they change, so it reads nothing itself.
/// </summary>
public sealed partial class SetupSummary : ObservableObject
{
    private readonly ModsTabViewModel _mods;

    public SetupSummary(ModsTabViewModel mods)
    {
        ArgumentNullException.ThrowIfNull(mods);
        _mods = mods;
        NotifyCollectionChangedEventHandler changed = (_, _) => Update();
        mods.Library.Mods.CollectionChanged += changed;
        mods.HandInstalled.Candidates.CollectionChanged += changed;
        mods.LoadOrder.Conflicts.CollectionChanged += changed;
        mods.LoadOrder.PropertyChanged += OnLoadOrderChanged;
        Update();
    }

    public ObservableCollection<SetupPart> Parts { get; } = [];

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Body { get; set; } = string.Empty;

    private void OnLoadOrderChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LoadOrderViewModel.CombinedCount))
        {
            Update();
        }
    }

    private void Update()
    {
        var library = _mods.Library.Mods;
        var found = _mods.HandInstalled.Candidates;
        var overlaps = _mods.LoadOrder.Conflicts.Count(c => !c.SameContent);
        var combined = _mods.LoadOrder.CombinedCount;
        SetupPart[] parts =
        [
            new("Add-ins", Count(library.Count(m => m.IsAddIn && m.IsActive), found.Count(m => m.IsAddIn)), "Ready", true, HomeTarget.ModsLibrary),
            new(
                "Override mods",
                Count(library.Count(m => !m.IsAddIn && m.IsActive), found.Count(m => !m.IsAddIn)),
                overlaps == 0 ? "Ready" : Counted.Of(overlaps, "overlap"),
                overlaps == 0,
                overlaps == 0 ? HomeTarget.ModsLibrary : HomeTarget.ModsArrange),
            new("Combined files", combined == 0 ? "None needed" : $"{combined} combined", "Ready", true, HomeTarget.ModsArrange),
        ];
        Parts.Clear();
        foreach (var part in parts)
        {
            Parts.Add(part);
        }

        var ready = parts.All(p => p.IsReady);
        Title = ready ? "Everything is in place" : "Almost there";
        Body = ready
            ? "Your add-ins, override mods and combined files are all set."
            : "Some mods change the same file. Arrange them to choose which one wins.";
    }

    private static string Count(int on, int found) =>
        found == 0 ? $"{on} on" : $"{on} on · {found} found in your game";
}
