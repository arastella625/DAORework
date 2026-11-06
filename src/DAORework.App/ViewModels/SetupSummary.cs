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
        mods.LoadOrder.PropertyChanged += OnLoadOrderChanged;
        Update();
    }

    public ObservableCollection<SetupPart> Parts { get; } = [];

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Body { get; set; } = string.Empty;

    /// <summary>Gets a value indicating whether any mod is on, so a problem could come from one.</summary>
    [ObservableProperty]
    public partial bool HasModsOn { get; private set; }

    private void OnLoadOrderChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LoadOrderViewModel.CombinedCount) or nameof(LoadOrderViewModel.OverlapCount) or nameof(LoadOrderViewModel.SharedCheck))
        {
            Update();
        }
    }

    private void Update()
    {
        var library = _mods.Library.Mods;
        var found = _mods.Filter.Unlisted;
        HasModsOn = library.Any(m => m.IsActive);
        var check = HasModsOn ? _mods.LoadOrder.SharedCheck : SharedFilesCheck.Done; // With nothing on, nothing can be shared.
        SetupPart[] parts =
        [
            new("Add-ins", Count(library.Count(m => m.IsAddIn && m.IsActive), found.Count(m => m.IsAddIn)), "Ready", true, HomeTarget.ModsLibrary, ModKindFilter.AddIns),
            OverrideMods(Count(library.Count(m => !m.IsAddIn && m.IsActive), found.Count(m => !m.IsAddIn)), check),
            CombinedFiles(check),
        ];
        Parts.Clear();
        foreach (var part in parts)
        {
            Parts.Add(part);
        }

        Say(parts.All(p => p.IsReady), found.Count > 0, library.Count == 0 && found.Count == 0, check);
    }

    private SetupPart OverrideMods(string count, SharedFilesCheck check)
    {
        var overlaps = _mods.LoadOrder.OverlapCount;
        return new(
            "Override mods",
            count,
            Unknown(check) ?? (overlaps == 0 ? "Ready" : Counted.Of(overlaps, "overlap")),
            check == SharedFilesCheck.Done && overlaps == 0,
            overlaps == 0 ? HomeTarget.ModsLibrary : HomeTarget.ModsArrange,
            ModKindFilter.OverrideMods,
            check == SharedFilesCheck.Checking);
    }

    private SetupPart CombinedFiles(SharedFilesCheck check) => new(
        "Combined files",
        check == SharedFilesCheck.Done ? CombinedCount(_mods.LoadOrder.CombinedCount, _mods.LoadOrder.OverlapCount) : "Not known yet",
        Unknown(check) ?? "Ready",
        check == SharedFilesCheck.Done,
        HomeTarget.ModsCombined,
        ModKindFilter.CombinedFiles,
        check == SharedFilesCheck.Checking);

    private void Say(bool ready, bool unmanaged, bool noMods, SharedFilesCheck check)
    {
        var checking = check == SharedFilesCheck.Checking && !unmanaged;
        Title = noMods ? "No mods yet" : checking ? "Checking your mods" : ready && !unmanaged ? "Everything is in place" : "Almost there";
        Body = (noMods, ready, unmanaged) switch
        {
            (true, _, _) => "Your mods will show here once you've added some.",
            (_, _, true) => "Some mods in your game aren't looked after by DAORework yet.",
            (_, true, _) => "Your add-ins, override mods and combined files are all set.",
            _ when checking => "Seeing which files your mods share. The first look can take a minute.",
            _ when check == SharedFilesCheck.Failed => "DAORework couldn't check which files your mods share. Combined files says why.",
            _ => "Some mods change the same file. Arrange them to choose which one wins.",
        };
    }

    /// <summary>What a card says in place of <i>Ready</i> until the shared files are known.</summary>
    private static string? Unknown(SharedFilesCheck check) => check switch
    {
        SharedFilesCheck.Checking => "Checking",
        SharedFilesCheck.Failed => "Couldn't check",
        _ => null,
    };

    /// <summary>The card lists files mods share as well as merged ones, so "None needed" is only true of neither.</summary>
    private static string CombinedCount(int combined, int shared) => (combined, shared) switch
    {
        (0, 0) => "None needed",
        (0, _) => $"{shared} shared",
        (_, 0) => $"{combined} combined",
        _ => $"{combined} combined · {shared} shared",
    };

    private static string Count(int on, int found) =>
        found == 0 ? $"{on} on" : $"{on} on · {found} found in your game";
}
