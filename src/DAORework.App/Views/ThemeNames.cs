using Avalonia.Data.Converters;
using DAORework.App.Themes;

namespace DAORework.App.Views;

/// <summary>Shows a theme's name for its id, as a look card says which theme it starts from.</summary>
public static class ThemeNames
{
    public static FuncValueConverter<string?, string> Of { get; } = new(id => AppThemes.Find(id)?.Name ?? id ?? string.Empty);
}
