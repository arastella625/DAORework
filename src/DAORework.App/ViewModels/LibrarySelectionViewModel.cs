using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.Core.Mods.Planning;

namespace DAORework.App.ViewModels;

/// <summary>
/// The Library's tick boxes (P2.5-14): <i>Activate selected</i> plans every ticked mod that is not active, and
/// <i>Deactivate selected</i> every ticked mod that is, each as one change, as <c>mods activate &lt;id&gt;...</c> does.
/// The plan is shown by <see cref="ModActivationViewModel"/> like any other, and nothing changes until <i>Apply</i>.
/// </summary>
public sealed partial class LibrarySelectionViewModel : ObservableObject
{
    private readonly ObservableCollection<LibraryModRow> _mods;
    private readonly ObservableCollection<LibraryModRow> _visible;
    private readonly ModActivationViewModel _activation;

    public LibrarySelectionViewModel(ObservableCollection<LibraryModRow> mods, ObservableCollection<LibraryModRow> visible, ModActivationViewModel activation)
    {
        ArgumentNullException.ThrowIfNull(mods);
        _mods = mods;
        _visible = visible;
        _activation = activation;
        _mods.CollectionChanged += OnModsChanged;
        foreach (var mod in _mods)
        {
            mod.PropertyChanged += OnModChanged;
        }
    }

    /// <summary>Gets a value indicating whether a ticked mod is not active yet.</summary>
    public bool CanActivateSelected => _mods.Any(m => m.IsSelected && !m.IsActive);

    /// <summary>Gets a value indicating whether a ticked mod is active.</summary>
    public bool CanDeactivateSelected => _mods.Any(m => m.IsSelected && m.IsActive);

    [RelayCommand(CanExecute = nameof(CanActivateSelected))]
    private Task ActivateSelectedAsync(CancellationToken cancellationToken) =>
        _activation.PlanChangeAsync(ModChangeKind.Activate, [.. _mods.Where(m => m.IsSelected && !m.IsActive)], cancellationToken);

    [RelayCommand(CanExecute = nameof(CanDeactivateSelected))]
    private Task DeactivateSelectedAsync(CancellationToken cancellationToken) =>
        _activation.PlanChangeAsync(ModChangeKind.Deactivate, [.. _mods.Where(m => m.IsSelected && m.IsActive)], cancellationToken);

    /// <summary>Ticks every mod shown, so a search or a chip narrows it (P2.6-30), but only one copy of a mod held twice (P2.6-18): those that are on, or else the newest.</summary>
    [RelayCommand]
    private void SelectAll()
    {
        foreach (var mod in _visible)
        {
            mod.IsSelected = true;
        }

        foreach (var copies in _visible.Where(m => m.Copy is not null).GroupBy(m => m.Copy!.Group, StringComparer.OrdinalIgnoreCase))
        {
            var keep = copies.Any(m => m.IsActive) ? 0 : 1;
            foreach (var extra in copies.Where(m => !m.IsActive).OrderBy(m => m.Copy!.Rank).Skip(keep))
            {
                extra.IsSelected = false;
            }
        }
    }

    [RelayCommand]
    private void ClearSelection() => SetSelected(false);

    private void SetSelected(bool selected)
    {
        foreach (var mod in _mods)
        {
            mod.IsSelected = selected;
        }
    }

    private void OnModsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Reading the library again replaces every row at once, so a Reset brings new rows to watch.
        foreach (var mod in e.Action == NotifyCollectionChangedAction.Reset ? _mods : e.NewItems?.OfType<LibraryModRow>() ?? [])
        {
            mod.PropertyChanged -= OnModChanged; // A row kept from before is already watched.
            mod.PropertyChanged += OnModChanged;
        }

        foreach (var mod in e.OldItems?.OfType<LibraryModRow>() ?? [])
        {
            mod.PropertyChanged -= OnModChanged;
        }

        SelectionChanged();
    }

    private void OnModChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LibraryModRow.IsSelected) or nameof(LibraryModRow.IsActive))
        {
            SelectionChanged();
        }
    }

    private void SelectionChanged()
    {
        OnPropertyChanged(nameof(CanActivateSelected));
        OnPropertyChanged(nameof(CanDeactivateSelected));
        ActivateSelectedCommand.NotifyCanExecuteChanged();
        DeactivateSelectedCommand.NotifyCanExecuteChanged();
    }
}
