using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Core.Mods.AddIns;

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

    /// <summary>Raised when Home opens the page, which should start at the top and not where it was left (P2.6-63).</summary>
    public event EventHandler? OpenedFromHome;

    public RowList<LibraryModRow> VisibleMods { get; } = [];

    public RowList<HandInstalledModRow> VisibleFound { get; } = [];

    /// <summary>Gets what is found in the game and not already listed as a mod, whatever a search or a chip shows.</summary>
    public IReadOnlyList<HandInstalledModRow> Unlisted { get; private set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsList), nameof(ShowsMods), nameof(ShowsFound), nameof(ShowsCombined), nameof(KindNote), nameof(HasKind))]
    public partial ModKindFilter Kind { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsMods), nameof(ShowsFound), nameof(IsAll), nameof(IsOn), nameof(IsOff), nameof(IsFound), nameof(IsGameFolder), nameof(IsSharingAddIns))]
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

    /// <summary>Gets or sets each enabled add-in's Priority and shared files, by UID, which its row shows (P2.5-23).</summary>
    [ObservableProperty]
    public partial IReadOnlyDictionary<string, AddInMark> AddInMarks { get; set; } = new Dictionary<string, AddInMark>();

    [ObservableProperty]
    public partial string SharingAddInsLabel { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether two add-ins share a file, so its chip shows (P2.5-23).</summary>
    [ObservableProperty]
    public partial bool HasSharingAddIns { get; set; }

    public bool ShowsList => Kind != ModKindFilter.CombinedFiles && !Arranging;

    public bool ShowsMods => ShowsList && Show is not (ModShowFilter.Found or ModShowFilter.GameFolder);

    public bool ShowsFound => ShowsList && Show is ModShowFilter.All or ModShowFilter.Found or ModShowFilter.GameFolder or ModShowFilter.SharingAddIns;

    public bool ShowsCombined => Kind == ModKindFilter.CombinedFiles && !Arranging;

    public bool IsAll => Show == ModShowFilter.All;

    public bool IsOn => Show == ModShowFilter.On;

    public bool IsOff => Show == ModShowFilter.Off;

    public bool IsFound => Show == ModShowFilter.Found;

    public bool IsGameFolder => Show == ModShowFilter.GameFolder;

    public bool IsSharingAddIns => Show == ModShowFilter.SharingAddIns;

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

    partial void OnAddInMarksChanged(IReadOnlyDictionary<string, AddInMark> value) => Update();

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

    /// <summary>Tells the page Home has just opened it.</summary>
    public void NoteOpenedFromHome() => OpenedFromHome?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void ToggleArrange() => Arranging = !Arranging;

    [RelayCommand]
    private void ShowEveryKind() => Kind = ModKindFilter.All;

    // A row's IsActive changes when a change is applied, without the list itself changing.
    private void OnModsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Reading the library again replaces every row at once, so a Reset brings new rows to watch.
        var added = e.Action == NotifyCollectionChangedAction.Reset ? _mods : e.NewItems?.OfType<LibraryModRow>() ?? [];
        foreach (var row in added)
        {
            row.PropertyChanged -= OnRowChanged; // A row kept from before is already watched.
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
        MarkAddIns();
        Unlisted = [.. _found.Where(m => !alreadyListed.Contains(m))];
        var mods = _mods.Where(m => OfKind(m.IsAddIn)).ToList();
        var found = _found.Where(m => OfKind(m.IsAddIn) && !alreadyListed.Contains(m)).ToList();
        Count(mods, found);
        if (ChipHasGone())
        {
            Show = ModShowFilter.All; // Nothing is left under the chip that was picked.
        }

        Refill(VisibleMods, ModListOrder.Sorted(mods.Where(m => ModListOrder.Matches(m.Title, SearchText) && Shows(m)), (ModSort)SortIndex));
        Refill(VisibleFound, found.Where(m => ModListOrder.Matches(m.Title, SearchText) && Shows(m)));
        NothingMatches = !string.IsNullOrWhiteSpace(SearchText) && VisibleMods.Count == 0 && VisibleFound.Count == 0;
    }

    private void Count(List<LibraryModRow> mods, List<HandInstalledModRow> found)
    {
        var on = mods.Count(m => m.IsActive);
        AllLabel = $"All ({mods.Count + found.Count})";
        OnLabel = $"On ({on})";
        OffLabel = $"Off ({mods.Count - on})";
        FoundLabel = $"Found in your game ({found.Count})";
        GameFolderLabel = $"In your game folder ({found.Count(m => m.IsInGameFolder)})";
        HasGameFolder = _found.Any(m => m.IsInGameFolder);
        SharingAddInsLabel = $"Add-ins sharing files ({mods.Count(m => Shares(m.PriorityMark)) + found.Count(m => Shares(m.PriorityMark))})";
        HasSharingAddIns = _mods.Any(m => Shares(m.PriorityMark)) || _found.Any(m => Shares(m.PriorityMark));
    }

    private bool ChipHasGone() => Show switch
    {
        ModShowFilter.GameFolder => !HasGameFolder,
        ModShowFilter.SharingAddIns => !HasSharingAddIns,
        _ => false,
    };

    private bool Shows(LibraryModRow mod) => Show switch
    {
        ModShowFilter.On => mod.IsActive,
        ModShowFilter.Off => !mod.IsActive,
        ModShowFilter.SharingAddIns => Shares(mod.PriorityMark),
        _ => true,
    };

    private bool Shows(HandInstalledModRow row) => Show switch
    {
        ModShowFilter.GameFolder => row.IsInGameFolder,
        ModShowFilter.SharingAddIns => Shares(row.PriorityMark),
        _ => true,
    };

    private static bool Shares(AddInMark? mark) => mark?.Shares == true;

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

    /// <summary>Each add-in row shows its Priority, whichever list it is in and however often the lists are read again (P2.5-23).</summary>
    private void MarkAddIns()
    {
        foreach (var mod in _mods)
        {
            mod.PriorityMark = mod.AddInUid is { } uid ? AddInMarks.GetValueOrDefault(uid) : null;
        }

        foreach (var row in _found)
        {
            row.PriorityMark = row.AddInUid is { } uid ? AddInMarks.GetValueOrDefault(uid) : null;
        }
    }

    /// <summary>Changes the list only if what it shows has changed: building every row again is the slowest thing the window does.</summary>
    private static void Refill<T>(RowList<T> target, IEnumerable<T> items)
    {
        var rows = items.ToList();
        if (!target.SequenceEqual(rows))
        {
            target.ReplaceAll(rows);
        }
    }
}
