using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DAORework.App.ViewModels;

/// <summary>
/// The Mods tab as one page (ADR-0026, P2.5-26): the three kind cards and the All, On, Off and Found chips filter the
/// Library and the mods found in the game, and <i>Arrange</i> turns the page into the load order. It only filters what
/// the panels already hold.
/// </summary>
public sealed partial class ModListFilter : ObservableObject
{
    private readonly ObservableCollection<LibraryModRow> _mods;
    private readonly ObservableCollection<HandInstalledModRow> _found;

    public ModListFilter(ObservableCollection<LibraryModRow> mods, ObservableCollection<HandInstalledModRow> found)
    {
        ArgumentNullException.ThrowIfNull(mods);
        ArgumentNullException.ThrowIfNull(found);
        _mods = mods;
        _found = found;
        mods.CollectionChanged += OnModsChanged;
        found.CollectionChanged += (_, _) => Update();
        Update();
    }

    public ObservableCollection<LibraryModRow> VisibleMods { get; } = [];

    public ObservableCollection<HandInstalledModRow> VisibleFound { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsList), nameof(ShowsMods), nameof(ShowsFound), nameof(ShowsCombined), nameof(KindNote), nameof(HasKind))]
    public partial ModKindFilter Kind { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsMods), nameof(ShowsFound), nameof(IsAll), nameof(IsOn), nameof(IsOff), nameof(IsFound), nameof(IsGameFolder))]
    public partial ModShowFilter Show { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsList), nameof(ShowsMods), nameof(ShowsFound), nameof(ShowsCombined), nameof(ArrangeLabel))]
    public partial bool Arranging { get; set; }

    /// <summary>Gets or sets what the list is narrowed to, by name (P2.6-30).</summary>
    [ObservableProperty]
    public partial string? SearchText { get; set; }

    /// <summary>Gets or sets the sort box's choice, which is a <see cref="ModSort"/>.</summary>
    [ObservableProperty]
    public partial int SortIndex { get; set; }

    /// <summary>Gets a value indicating whether a search found nothing, so the page can say so.</summary>
    [ObservableProperty]
    public partial bool NothingMatches { get; private set; }

    [ObservableProperty]
    public partial string AllLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OnLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OffLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FoundLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string GameFolderLabel { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the game folder's override holds anything, so its chip shows (P2.5-22).</summary>
    [ObservableProperty]
    public partial bool HasGameFolder { get; set; }

    public bool ShowsList => Kind != ModKindFilter.CombinedFiles && !Arranging;

    public bool ShowsMods => ShowsList && Show is not (ModShowFilter.Found or ModShowFilter.GameFolder);

    public bool ShowsFound => ShowsList && Show is ModShowFilter.All or ModShowFilter.Found or ModShowFilter.GameFolder;

    public bool ShowsCombined => Kind == ModKindFilter.CombinedFiles && !Arranging;

    public bool IsAll => Show == ModShowFilter.All;

    public bool IsOn => Show == ModShowFilter.On;

    public bool IsOff => Show == ModShowFilter.Off;

    public bool IsFound => Show == ModShowFilter.Found;

    public bool IsGameFolder => Show == ModShowFilter.GameFolder;

    public string ArrangeLabel => Arranging ? "Done arranging" : "Arrange";

    /// <summary>Gets which kind a card narrowed the page to, or null when every kind shows.</summary>
    public string? KindNote => Kind switch
    {
        ModKindFilter.AddIns => "Showing add-ins only.",
        ModKindFilter.OverrideMods => "Showing override mods only.",
        ModKindFilter.CombinedFiles => "Showing the files more than one mod changes.",
        _ => null,
    };

    public bool HasKind => Kind != ModKindFilter.All;

    partial void OnKindChanged(ModKindFilter value) => Update();

    partial void OnShowChanged(ModShowFilter value) => Update();

    partial void OnSearchTextChanged(string? value) => Update();

    partial void OnSortIndexChanged(int value) => Update();

    /// <summary>Shows only the mods in one state; the list comes back if the Combined files card was showing.</summary>
    [RelayCommand]
    private void ShowOnly(ModShowFilter show)
    {
        Show = show;
        Arranging = false;
        if (Kind == ModKindFilter.CombinedFiles)
        {
            Kind = ModKindFilter.All;
        }
    }

    /// <summary>Shows one kind; picking the same card again shows every kind.</summary>
    [RelayCommand]
    private void PickKind(ModKindFilter kind)
    {
        Kind = Kind == kind ? ModKindFilter.All : kind;
        Arranging = false;
    }

    [RelayCommand]
    private void ToggleArrange() => Arranging = !Arranging;

    [RelayCommand]
    private void ShowEveryKind() => Kind = ModKindFilter.All;

    // A row's IsActive changes when a change is applied, without the list itself changing.
    private void OnModsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var row in e.NewItems?.OfType<LibraryModRow>() ?? [])
        {
            row.PropertyChanged += OnRowChanged;
        }

        Update();
    }

    private void OnRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LibraryModRow.IsActive))
        {
            Update();
        }
    }

    private bool OfKind(bool isAddIn) => Kind switch
    {
        ModKindFilter.AddIns => isAddIn,
        ModKindFilter.OverrideMods => !isAddIn,
        _ => true,
    };

    private void Update()
    {
        var alreadyListed = MarkModsInTheGame();
        var mods = _mods.Where(m => OfKind(m.IsAddIn)).ToList();
        var found = _found.Where(m => OfKind(m.IsAddIn) && !alreadyListed.Contains(m)).ToList();
        var on = mods.Count(m => m.IsActive);
        AllLabel = $"All ({mods.Count + found.Count})";
        OnLabel = $"On ({on})";
        OffLabel = $"Off ({mods.Count - on})";
        FoundLabel = $"Found in your game ({found.Count})";
        var inGameFolder = found.Where(m => m.IsInGameFolder).ToList();
        GameFolderLabel = $"In your game folder ({inGameFolder.Count})";
        HasGameFolder = _found.Any(m => m.IsInGameFolder);
        if (!HasGameFolder && Show == ModShowFilter.GameFolder)
        {
            Show = ModShowFilter.All; // The chip has gone, and nothing is left under it.
        }

        var shown = mods.Where(m => ModListOrder.Matches(m.Title, SearchText) && Show switch
        {
            ModShowFilter.On => m.IsActive,
            ModShowFilter.Off => !m.IsActive,
            _ => true,
        });
        Refill(VisibleMods, ModListOrder.Sorted(shown, (ModSort)SortIndex));
        Refill(VisibleFound, (Show == ModShowFilter.GameFolder ? inGameFolder : found).Where(m => ModListOrder.Matches(m.Title, SearchText)));
        NothingMatches = !string.IsNullOrWhiteSpace(SearchText) && VisibleMods.Count == 0 && VisibleFound.Count == 0;
    }

    /// <summary>
    /// A mod in the Library and in the game is one mod, shown once (P2.6-26): an off row says so, and turning it on takes
    /// over what is there. One that is on stays under Found, where its leftovers are cleared, and so does one Vortex holds.
    /// </summary>
    private HashSet<HandInstalledModRow> MarkModsInTheGame()
    {
        var off = _mods.Where(m => !m.IsActive).ToDictionary(m => m.Id, StringComparer.OrdinalIgnoreCase);
        var listed = _found.Where(f => f.Candidate is { DeployedByVortex: false, LibraryMatch: { } match } && off.ContainsKey(match.ModId)).ToHashSet();
        var inGame = listed.Select(f => f.Candidate!.LibraryMatch!.ModId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var mod in _mods)
        {
            mod.IsInGame = inGame.Contains(mod.Id);
        }

        return listed;
    }

    private static void Refill<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }
}
