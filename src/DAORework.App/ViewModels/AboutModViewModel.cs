using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DAORework.App.ViewModels;

/// <summary>
/// <i>About this mod</i> (ADR-0050 §3, P2.10-05): a mod's own page, shown on the Mods tab in place of the list. It
/// replaces ADR-0038's expand in place: the row stays one line, and what it used to unfold is under <i>Details</i>.
/// The page follows the Library, so reading it again shows the same mod's new row, and a mod that has gone closes it.
/// </summary>
public sealed partial class AboutModViewModel : ObservableObject
{
    private readonly ObservableCollection<LibraryModRow> _mods;

    public AboutModViewModel(ObservableCollection<LibraryModRow> mods)
    {
        ArgumentNullException.ThrowIfNull(mods);
        _mods = mods;
        _mods.CollectionChanged += Follow;
    }

    /// <summary>Raised when the page opens or closes, so the view can keep the list's place.</summary>
    public event EventHandler? OpenedOrClosed;

    /// <summary>Gets the mod shown, or null when the list is.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOpen), nameof(Kind), nameof(IsOverride))]
    public partial LibraryModRow? Mod { get; private set; }

    /// <summary>Gets or sets a value indicating whether <i>Details</i> is open.</summary>
    [ObservableProperty]
    public partial bool ShowsDetails { get; set; }

    public bool IsOpen => Mod is not null;

    /// <summary>Gets what kind of mod it is, in docs/VOICE.md's words.</summary>
    public string Kind => Mod switch
    {
        null => string.Empty,
        { RunsInGame: true } => "Runs in the game",
        { IsAddIn: true } => "Add-in",
        _ => "Override mod",
    };

    /// <summary>Gets a value indicating whether it is an override mod, so its crest shows that kind's mark.</summary>
    public bool IsOverride => Mod is { IsAddIn: false, RunsInGame: false };

    [RelayCommand]
    private void Open(LibraryModRow mod)
    {
        ArgumentNullException.ThrowIfNull(mod);
        ShowsDetails = false;
        Mod = mod;
        OpenedOrClosed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary><i>Back to your mods</i>: the list, at the place it was left.</summary>
    [RelayCommand]
    private void Back()
    {
        if (Mod is null)
        {
            return;
        }

        Mod = null;
        OpenedOrClosed?.Invoke(this, EventArgs.Empty);
    }

    // The Library replaces its rows when it reads again; the page moves to the same mod's new row, or closes.
    private void Follow(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Mod is not { } shown)
        {
            return;
        }

        if (_mods.FirstOrDefault(m => m.Id == shown.Id) is { } same)
        {
            Mod = same;
        }
        else
        {
            Back();
        }
    }
}
