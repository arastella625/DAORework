using DAORework.Application.Preferences;
using DAORework.Core.Common;
using DAORework.Core.Preferences;

namespace DAORework.App.Themes;

/// <summary>
/// Applies the saved theme or look when the app starts and saves the theme the user picks (ADR-0016). What cannot be
/// shown falls back to the default with a warning in the log, not an error (<see cref="LookPresenter"/>).
/// </summary>
public sealed class ThemePreference
{
    private readonly ThemeSwitcher _switcher;
    private readonly LookPresenter _presenter;
    private readonly SetLook _setLook;

    public ThemePreference(ThemeSwitcher switcher, LookPresenter presenter, SetLook setLook)
    {
        _switcher = switcher;
        _presenter = presenter;
        _setLook = setLook;
    }

    /// <summary>Raised after a theme is applied, whether the user picked it or it was the saved one.</summary>
    public event EventHandler<AppTheme>? Applied
    {
        add => _switcher.Applied += value;
        remove => _switcher.Applied -= value;
    }

    public AppTheme Current => _switcher.Current;

    /// <summary>Shows the app-wide look, before any profile is selected.</summary>
    public Task ApplySavedAsync(CancellationToken cancellationToken = default) => _presenter.ShowForProfileAsync(null, cancellationToken);

    /// <summary>Shows the app-wide look or theme <paramref name="lookId"/> (null for the default), as after a look is deleted.</summary>
    public Task ShowAppWideAsync(string? lookId, CancellationToken cancellationToken = default) => _presenter.ShowAsync(lookId, cancellationToken);

    /// <summary>Shows a custom look without saving it, to preview it while it is edited.</summary>
    public Task PreviewAsync(string lookId, CancellationToken cancellationToken = default) => _presenter.ShowAsync(lookId, cancellationToken);

    /// <summary>Makes a custom look the app-wide one: shown at once, then saved.</summary>
    public async Task<Result<AppPreferences>> ChooseLookAsync(string lookId, CancellationToken cancellationToken = default)
    {
        await _presenter.ShowAsync(lookId, cancellationToken);
        return await _setLook.ExecuteAsync(new SetLookRequest(lookId), cancellationToken);
    }

    /// <summary>Shows <paramref name="theme"/> at once, then saves it.</summary>
    public Task<Result<AppPreferences>> ChooseAsync(AppTheme theme, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(theme);
        _switcher.Apply(theme);
        return _setLook.ExecuteAsync(new SetLookRequest(theme.Id), cancellationToken);
    }
}
