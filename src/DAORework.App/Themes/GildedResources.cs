using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace DAORework.App.Themes;

/// <summary>
/// The gilded-tome style's brushes (ADR-0026 §4), derived from a theme's tokens so every theme and custom look recolors
/// them: the card's fall of light, the inner gold rule, the button's inset rules, the switch's gem and pewter knobs, and
/// <c>OrnamentOpacity</c>, which a plain theme (no glow, such as Korcari) sets to 0 to drop the ornaments.
/// </summary>
internal static class GildedResources
{
    public const string OrnamentOpacity = nameof(OrnamentOpacity);

    public static void Add(ResourceDictionary theme, double glowRadius)
    {
        var background = ThemeResources.ColorOf(theme, "Background");
        var gold = ThemeResources.ColorOf(theme, "Heading");
        var accent = ThemeResources.ColorOf(theme, "Accent");
        var glow = ThemeResources.ColorOf(theme, "FocusGlow");
        var secondary = ThemeResources.ColorOf(theme, "TextSecondary");

        theme["CardBackgroundBrush"] = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(ThemeResources.ColorOf(theme, "SurfaceRaised"), 0),
                new GradientStop(ThemeResources.ColorOf(theme, "Surface"), 0.45),
            },
        };
        theme["InnerRuleBrush"] = new SolidColorBrush(Color.FromArgb(0x38, gold.R, gold.G, gold.B));
        theme["ButtonRuleShadow"] = new BoxShadows(
            new BoxShadow { IsInset = true, Spread = 2, Color = Color.FromArgb(0x8C, background.R, background.G, background.B) },
            [new BoxShadow { IsInset = true, Spread = 3, Color = Color.FromArgb(0x40, gold.R, gold.G, gold.B) }]);
        theme["GemOnBrush"] = Gem(Lighter(glow), glow, accent, Darker(accent));
        theme["GemOffBrush"] = Gem(Lighter(secondary), secondary, Darker(secondary), background);
        theme[OrnamentOpacity] = glowRadius > 0 ? 1d : 0d;
    }

    private static RadialGradientBrush Gem(Color highlight, Color light, Color body, Color edge) => new()
    {
        GradientOrigin = new RelativePoint(0.35, 0.3, RelativeUnit.Relative),
        Center = new RelativePoint(0.45, 0.45, RelativeUnit.Relative),
        RadiusX = new RelativeScalar(0.6, RelativeUnit.Relative),
        RadiusY = new RelativeScalar(0.6, RelativeUnit.Relative),
        GradientStops = { new GradientStop(highlight, 0), new GradientStop(light, 0.3), new GradientStop(body, 0.7), new GradientStop(edge, 1) },
    };

    private static Color Lighter(Color color) =>
        Color.FromRgb((byte)((color.R + 255) / 2), (byte)((color.G + 255) / 2), (byte)((color.B + 255) / 2));

    private static Color Darker(Color color) => Color.FromRgb((byte)(color.R / 2), (byte)(color.G / 2), (byte)(color.B / 2));
}
