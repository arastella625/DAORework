using Avalonia.Media;
using DAORework.Application.Looks;
using DAORework.Application.Preferences;
using DAORework.Core.Looks;
using Microsoft.Extensions.Logging;

namespace DAORework.App.Themes;

/// <summary>
/// Shows the look that belongs on screen (ADR-0017): the selected profile's own look, or the app-wide one. A look or
/// picture that cannot be shown falls back with a warning in the log (to the default theme, or to the look without its
/// picture), never an error the user has to deal with.
/// </summary>
public sealed class LookPresenter
{
    private readonly ThemeSwitcher _switcher;
    private readonly GetPreferences _getPreferences;
    private readonly LoadLook _loadLook;
    private readonly ILogger<LookPresenter> _logger;

    public LookPresenter(ThemeSwitcher switcher, GetPreferences getPreferences, LoadLook loadLook, ILogger<LookPresenter> logger)
    {
        _switcher = switcher;
        _getPreferences = getPreferences;
        _loadLook = loadLook;
        _logger = logger;
    }

    /// <summary>Shows <paramref name="profileName"/>'s own look, or the app-wide one when it has none (or no profile is selected).</summary>
    public async Task ShowForProfileAsync(string? profileName, CancellationToken cancellationToken = default)
    {
        if (!(await _getPreferences.ExecuteAsync(new GetPreferencesRequest(), cancellationToken)).TryGetValue(out var preferences, out var error))
        {
            _logger.LogWarning("Using the default theme: {Code} {Message}", error.Code, error.Message);
            _switcher.Apply(AppThemes.Default);
            return;
        }

        var lookId = profileName is not null && preferences.ProfileLooks.TryGetValue(profileName, out var own) ? own : preferences.Look;
        await ShowAsync(lookId, cancellationToken);
    }

    /// <summary>Shows a built-in theme or a custom look by id; null is the default theme.</summary>
    public async Task ShowAsync(string? lookId, CancellationToken cancellationToken = default)
    {
        if (AppThemes.Find(lookId) is { } theme)
        {
            _switcher.Apply(theme);
            return;
        }

        if (lookId is null || !LookNames.IsValidId(lookId))
        {
            if (lookId is not null)
            {
                _logger.LogWarning("The saved theme {Theme} is not in this version; using the default.", lookId);
            }

            _switcher.Apply(AppThemes.Default);
            return;
        }

        if (!(await _loadLook.ExecuteAsync(new LoadLookRequest(lookId), cancellationToken)).TryGetValue(out var loaded, out var error))
        {
            _logger.LogWarning("The look {Look} cannot be shown; using the default: {Code} {Message}", lookId, error.Code, error.Message);
            _switcher.Apply(AppThemes.Default);
            return;
        }

        Show(loaded);
    }

    private void Show(LoadedLook loaded)
    {
        var look = loaded.Look;
        var baseTheme = AppThemes.Find(look.BaseTheme) ?? AppThemes.Default;
        var (dictionary, accentProblem) = LookAppearance.Build(_switcher.DictionaryOf(baseTheme), look);
        if (accentProblem is not null)
        {
            _logger.LogWarning("The look {Look} keeps its theme's accent: {Code} {Message}", look.Id, accentProblem.Code, accentProblem.Message);
        }

        if (loaded.PictureError is { } pictureError)
        {
            _logger.LogWarning("The look {Look} is shown without its picture: {Code} {Message}", look.Id, pictureError.Code, pictureError.Message);
        }

        LookPictureLayer? layer = null;
        if (loaded.Picture is { } bytes && look.Background is { } background)
        {
            try
            {
                layer = Layer(bytes, background, dictionary);
            }
            catch (InvalidDataException exception)
            {
                _logger.LogWarning(exception, "The look {Look} is shown without its picture, which could not be read.", look.Id);
            }
        }

        _switcher.ApplyLook(look.Id, baseTheme, dictionary, layer);
    }

    private static LookPictureLayer Layer(byte[] bytes, LookBackground background, Avalonia.Controls.ResourceDictionary dictionary)
    {
        var dark = LookAppearance.ToLook(dictionary, "Background");
        var accent = LookAppearance.ToLook(dictionary, "Accent");
        var floor = PictureFloor.Of(background, background.Tint, dictionary);
        var picture = LookPictureRenderer.Render(bytes, dark, accent, background.Tint);
        var stretch = background.Fit == BackgroundFit.Fit ? Stretch.Uniform : Stretch.UniformToFill;
        return new LookPictureLayer(picture, stretch, background.Blur, Math.Max(background.Dim, floor) / 100d);
    }
}
