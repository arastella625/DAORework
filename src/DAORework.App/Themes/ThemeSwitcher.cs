using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace DAORework.App.Themes;

/// <summary>
/// Registers every built-in theme under its own <see cref="ThemeVariant"/> and switches between them and custom looks
/// (ADR-0016, ADR-0017). Switching sets <c>RequestedThemeVariant</c>, so every <c>{DynamicResource}</c> follows. Fluent
/// reads its palette only when it loads, and accepts only Light and Dark palettes, so the switch also puts in a new
/// <see cref="FluentTheme"/> whose dark palette holds the colors. Every theme and look inherits Dark.
/// </summary>
public sealed class ThemeSwitcher
{
    private readonly Avalonia.Application _application;
    private readonly Dictionary<AppTheme, ResourceDictionary> _dictionaries = [];
    private ThemeVariant? _lookVariant;

    public ThemeSwitcher(Avalonia.Application application)
    {
        _application = application;
        application.Resources["GrainBrush"] = GrainTexture.CreateBrush();
        foreach (var theme in AppThemes.All)
        {
            var dictionary = (ResourceDictionary)AvaloniaXamlLoader.Load(theme.Source);
            ThemeResources.Complete(dictionary);
            application.Resources.ThemeDictionaries[theme.Variant] = dictionary;
            _dictionaries[theme] = dictionary;
        }

        Apply(AppThemes.Default);
    }

    /// <summary>Raised after a built-in theme is applied, so a picker can show it.</summary>
    public event EventHandler<AppTheme>? Applied;

    public AppTheme Current { get; private set; } = AppThemes.Default;

    /// <summary>Gets the custom look being shown, or null while a built-in theme is.</summary>
    public string? CurrentLookId { get; private set; }

    public ResourceDictionary DictionaryOf(AppTheme theme) => _dictionaries[theme];

    public void Apply(AppTheme theme)
    {
        Show(theme.Variant, _dictionaries[theme]);
        ShowPicture(null);
        Current = theme;
        CurrentLookId = null;
        Applied?.Invoke(this, theme);
    }

    /// <summary>Shows a custom look: its own color dictionary (made by <see cref="LookAppearance"/>) and, if any, its picture.</summary>
    public void ApplyLook(string lookId, AppTheme baseTheme, ResourceDictionary dictionary, LookPictureLayer? picture)
    {
        var variant = new ThemeVariant(lookId, ThemeVariant.Dark);
        if (_lookVariant is not null && !_lookVariant.Equals(variant))
        {
            _application.Resources.ThemeDictionaries.Remove(_lookVariant);
        }

        _application.Resources.ThemeDictionaries[variant] = dictionary;
        _lookVariant = variant;
        Show(variant, dictionary);
        ShowPicture(picture);
        Current = baseTheme;
        CurrentLookId = lookId;
    }

    private void Show(ThemeVariant variant, ResourceDictionary dictionary)
    {
        var palette = new ColorPaletteResources();
        FluentPalette.Fill(palette, dictionary);
        var fluent = new FluentTheme();
        fluent.Palettes[ThemeVariant.Dark] = palette;

        var styles = _application.Styles;
        styles[styles.IndexOf(styles.OfType<FluentTheme>().Single())] = fluent;
        _application.RequestedThemeVariant = variant;
    }

    // The Backdrop binds to these (Themes/Controls.axaml); with no picture it shows only the theme.
    private void ShowPicture(LookPictureLayer? picture)
    {
        var resources = _application.Resources;
        resources[LookPictureLayer.PictureKey] = picture?.Picture;
        resources[LookPictureLayer.StretchKey] = picture?.Stretch ?? Stretch.UniformToFill;
        resources[LookPictureLayer.BlurKey] = picture is { Blur: > 0 } ? new BlurEffect { Radius = picture.Blur } : null;
        resources[LookPictureLayer.VeilKey] = picture?.Veil ?? 0d;
    }
}
