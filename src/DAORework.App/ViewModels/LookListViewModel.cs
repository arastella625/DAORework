using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.App.Themes;
using DAORework.Application.Looks;
using DAORework.Core.Looks;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// The custom looks on the Settings page (ADR-0017): make one from the current theme, pick one to edit and preview,
/// make one the app-wide look, or delete one after a second click to confirm.
/// </summary>
public sealed partial class LookListViewModel : PageViewModel
{
    private readonly ListLooks _listLooks;
    private readonly CreateLook _createLook;
    private readonly DeleteLook _deleteLook;
    private readonly ThemePreference _themes;
    private bool _replacing;

    public LookListViewModel(ListLooks listLooks, CreateLook createLook, DeleteLook deleteLook, ThemePreference themes, ILogger<LookListViewModel> logger)
        : base(logger)
    {
        _listLooks = listLooks;
        _createLook = createLook;
        _deleteLook = deleteLook;
        _themes = themes;
    }

    /// <summary>Raised when the picked look changes, so the editor can show it (null when none is picked).</summary>
    public event EventHandler<Look?>? Picked;

    /// <summary>Raised after a look is made, saved or deleted, so a page offering looks can read them again.</summary>
    public event EventHandler? ListChanged;

    public ObservableCollection<Look> Looks { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    public partial Look? SelectedLook { get; set; }

    /// <summary>Gets or sets whether Delete was clicked once: the second click deletes.</summary>
    [ObservableProperty]
    public partial bool ConfirmingDelete { get; set; }

    public bool HasSelection => SelectedLook is not null;

    public Task LoadAsync(CancellationToken cancellationToken = default) =>
        GuardAsync(
            async token =>
            {
                var listing = (await _listLooks.ExecuteAsync(new ListLooksRequest(), token)).Value; // Listing never fails: bad looks are reported in it.
                Looks.Clear();
                foreach (var look in listing.Looks)
                {
                    Looks.Add(look);
                }

                if (listing.Unreadable.Count > 0)
                {
                    ReportError(listing.Unreadable[0]);
                }
            },
            cancellationToken);

    /// <summary>Puts a saved look back in the list, so its card shows the new name.</summary>
    public void Replace(Look look)
    {
        var index = Looks.ToList().FindIndex(existing => existing.Id == look.Id);
        if (index >= 0)
        {
            // The list drops its selection while the card is swapped; the editor already has the look and it is shown.
            _replacing = true;
            Looks[index] = look;
            SelectedLook = look;
            _replacing = false;
            ReportSuccess(string.Empty); // The editor says it was saved.
            ListChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    partial void OnSelectedLookChanged(Look? value)
    {
        if (_replacing)
        {
            return;
        }

        ConfirmingDelete = false;
        Picked?.Invoke(this, value);
        if (value is not null)
        {
            _ = _themes.PreviewAsync(value.Id);
        }
    }

    [RelayCommand]
    private Task NewLookAsync(CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                var name = $"My look {Looks.Count + 1}";
                if (!(await _createLook.ExecuteAsync(new CreateLookRequest(name, _themes.Current.Id), token)).TryGetValue(out var look, out var error))
                {
                    ReportError(error);
                    return;
                }

                Looks.Add(look);
                SelectedLook = look;
                ListChanged?.Invoke(this, EventArgs.Empty);
                ReportSuccess($"Made {look.Name} from {_themes.Current.Name}. Change it below.");
            },
            cancellationToken);

    [RelayCommand]
    private Task UseEverywhereAsync(CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                if (SelectedLook is not { } look)
                {
                    return;
                }

                if (!(await _themes.ChooseLookAsync(look.Id, token)).TryGetValue(out _, out var error))
                {
                    ReportError(error);
                    return;
                }

                ReportSuccess($"{look.Name} is now the app-wide look. Profiles with their own look keep it.");
            },
            cancellationToken);

    [RelayCommand]
    private Task DeleteAsync(CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                if (SelectedLook is not { } look)
                {
                    return;
                }

                if (!ConfirmingDelete)
                {
                    ConfirmingDelete = true;
                    ReportStatus($"Click Delete again to delete {look.Name} and its picture. Profiles that used it go back to the default.");
                    return;
                }

                if (!(await _deleteLook.ExecuteAsync(new DeleteLookRequest(look.Id), token)).TryGetValue(out var preferences, out var error))
                {
                    ReportError(error);
                    return;
                }

                Looks.Remove(look);
                SelectedLook = null;
                ListChanged?.Invoke(this, EventArgs.Empty);
                await _themes.ShowAppWideAsync(preferences.Look, token);
                ReportSuccess($"Deleted {look.Name}.");
            },
            cancellationToken);
}
