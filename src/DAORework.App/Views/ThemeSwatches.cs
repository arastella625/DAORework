using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using DAORework.App.Themes;

namespace DAORework.App.Views;

/// <summary>
/// A strip of a theme's own colors for its card on the Settings page: its background, card, accent, accent text and
/// heading brushes, read from that theme's dictionary, whatever theme is showing now.
/// </summary>
public static class ThemeSwatches
{
    private static readonly string[] _tokens = ["Background", "SurfaceRaised", "Accent", "AccentText", "Heading"];

    public static FuncValueConverter<AppTheme, IReadOnlyList<IBrush>> Of { get; } = new(theme =>
        theme is null
            ? []
            : _tokens.Select(token => (IBrush)Dictionary(theme)[ThemeTokens.BrushKey(token)]!).ToList()); // ThemeResources made every token's brush.

    private static ResourceDictionary Dictionary(AppTheme theme) =>
        (ResourceDictionary)Avalonia.Application.Current!.Resources.ThemeDictionaries[theme.Variant]; // The App registered every theme.
}
