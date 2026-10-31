using Avalonia.Data.Converters;
using Avalonia.Media;
using DAORework.Core.Looks;

namespace DAORework.App.Themes;

/// <summary>
/// Shows a user's accent pick as a swatch. These colors are the user's own data, not the theme's, which is why this
/// lives here and not under Views/, where only theme tokens are allowed (ADR-0016 rule 1).
/// </summary>
public static class LookBrushes
{
    public static FuncValueConverter<string?, IBrush?> FromHex { get; } = new(hex =>
        LookColor.Parse(hex).TryGetValue(out var color, out _) ? new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B)) : null);
}
