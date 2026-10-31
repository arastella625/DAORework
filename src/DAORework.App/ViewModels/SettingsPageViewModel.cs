using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DAORework.App.Themes;
using Microsoft.Extensions.Logging;

namespace DAORework.App.ViewModels;

/// <summary>
/// The Settings page: the theme picker (ADR-0016) and the custom looks with their pictures (ADR-0017). Picking a theme shows it at once
/// and saves it. The selection follows the theme that is applied, so the saved theme the app starts in shows as picked.
/// </summary>
public sealed partial class SettingsPageViewModel : PageViewModel
{
    private readonly ThemePreference _themes;
    private bool _following;

    public SettingsPageViewModel(
        ThemePreference themes, LookListViewModel looks, LookDetailsViewModel details, LookPictureViewModel picture, ILogger<SettingsPageViewModel> logger)
        : base(logger)
    {
        _themes = themes;
        Looks = looks;
        Details = details;
        Picture = picture;
        SelectedTheme = themes.Current;
        themes.Applied += (_, theme) => Follow(theme);
        looks.Picked += (_, look) =>
        {
            details.Edit(look);
            picture.Edit(look);
        };
        details.Saved += (_, look) =>
        {
            looks.Replace(look);
            picture.Edit(look); // A new base theme or accent can move the veil's floor.
        };
        picture.Changed += (_, look) => looks.Replace(look);
    }

    public LookListViewModel Looks { get; }

    public LookDetailsViewModel Details { get; }

    public LookPictureViewModel Picture { get; }

    public IReadOnlyList<AppTheme> Themes { get; } = AppThemes.All;

    [ObservableProperty]
    public partial AppTheme SelectedTheme { get; set; }

    public Task LoadAsync(CancellationToken cancellationToken = default) => Looks.LoadAsync(cancellationToken);

    partial void OnSelectedThemeChanged(AppTheme value)
    {
        if (!_following && value != _themes.Current)
        {
            _ = ChooseThemeCommand.ExecuteAsync(value);
        }
    }

    [RelayCommand]
    private Task ChooseThemeAsync(AppTheme theme, CancellationToken cancellationToken) =>
        GuardAsync(
            async token =>
            {
                if (!(await _themes.ChooseAsync(theme, token)).TryGetValue(out _, out var error))
                {
                    ReportError(error);
                    return;
                }

                ReportSuccess($"Saved: {theme.Name}. DAORework opens in it from now on.");
            },
            cancellationToken);

    private void Follow(AppTheme theme)
    {
        _following = true;
        SelectedTheme = theme;
        _following = false;
    }
}
