using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.App.Themes;
using DAORework.Application.Looks;
using DAORework.Application.Preferences;
using DAORework.Core.Looks;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// The look a launch profile shows while it is selected (ADR-0017): the app-wide one, a built-in theme, or one of the
/// user's looks. Picking one saves it and shows it at once.
/// </summary>
public sealed partial class ProfileLookViewModel : PageViewModel
{
    private readonly ListLooks _listLooks;
    private readonly GetPreferences _getPreferences;
    private readonly SetProfileLook _setProfileLook;
    private readonly LookPresenter _presenter;
    private bool _loading;

    public ProfileLookViewModel(ListLooks listLooks, GetPreferences getPreferences, SetProfileLook setProfileLook, LookPresenter presenter, ILogger<ProfileLookViewModel> logger)
        : base(logger)
    {
        _listLooks = listLooks;
        _getPreferences = getPreferences;
        _setProfileLook = setProfileLook;
        _presenter = presenter;
    }

    public ObservableCollection<LookChoice> Choices { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProfile))]
    public partial string? ProfileName { get; set; }

    [ObservableProperty]
    public partial LookChoice? SelectedChoice { get; set; }

    public bool HasProfile => ProfileName is not null;

    public Task LoadAsync(string? profileName, CancellationToken cancellationToken = default)
    {
        ProfileName = profileName;
        ReportSuccess(string.Empty);
        return RefreshAsync(cancellationToken);
    }

    /// <summary>Reads the looks and the profile's choice again, after looks are made, renamed or deleted in Settings.</summary>
    public Task RefreshAsync(CancellationToken cancellationToken = default) =>
        GuardAsync(
            async token =>
            {
                var looks = (await _listLooks.ExecuteAsync(new ListLooksRequest(), token)).Value.Looks; // Listing never fails.
                if (!(await _getPreferences.ExecuteAsync(new GetPreferencesRequest(), token)).TryGetValue(out var preferences, out var error))
                {
                    ReportError(error);
                    return;
                }

                _loading = true;
                Choices.Clear();
                Choices.Add(new LookChoice(null, "The app-wide look", $"Now {NameOf(preferences.Look, looks)}, set in Settings"));
                foreach (var theme in AppThemes.All)
                {
                    Choices.Add(new LookChoice(theme.Id, theme.Name, "Theme"));
                }

                foreach (var look in looks)
                {
                    Choices.Add(new LookChoice(look.Id, look.Name, $"Your look, from {NameOf(look.BaseTheme, looks)}"));
                }

                var own = ProfileName is not null && preferences.ProfileLooks.TryGetValue(ProfileName, out var id) ? id : null;
                SelectedChoice = Choices.FirstOrDefault(choice => choice.Id == own) ?? Choices[0]; // A look that is gone: the app-wide one.
                _loading = false;
            },
            cancellationToken);

    partial void OnSelectedChoiceChanged(LookChoice? value)
    {
        if (!_loading && value is not null)
        {
            _ = ChooseCommand.ExecuteAsync(value);
        }
    }

    [RelayCommand]
    private Task ChooseAsync(LookChoice choice, CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                if (ProfileName is null)
                {
                    return;
                }

                if (!(await _setProfileLook.ExecuteAsync(new SetProfileLookRequest(ProfileName, choice.Id), token)).TryGetValue(out _, out var error))
                {
                    ReportError(error);
                    return;
                }

                _loading = true;
                SelectedChoice = choice; // Already so when the user picked it; a choice made another way shows too.
                _loading = false;
                await _presenter.ShowForProfileAsync(ProfileName, token);
                ReportSuccess(choice.Id is null ? $"{ProfileName} uses the app-wide look." : $"{ProfileName} shows {choice.Name} while it is selected.");
            },
            cancellationToken);

    private static string NameOf(string? id, IReadOnlyCollection<Look> looks) =>
        AppThemes.Find(id)?.Name ?? looks.FirstOrDefault(look => look.Id == id)?.Name ?? AppThemes.Default.Name;
}
