using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace DAORework.App.Themes;

/// <summary>
/// Completes a theme's color file (ADR-0016): a brush for every token, the brushes derived from them (soft tints,
/// fades, the emphasis gradient, the smoke, the veil and the glow) and Fluent's accent-button and tab brushes. Fluent's
/// other colors come from its palette (<see cref="FluentPalette"/>).
/// </summary>
internal static class ThemeResources
{
    // Fluent's buttons and tabs read brushes rather than palette colors. The accent button's label must be OnAccent and
    // every button has a BorderStrong outline (contrast, rule 2), and the selected tab is underlined with the emphasis.
    private static readonly (string FluentKey, string Token)[] _fluentBrushes =
    [
        ("AccentButtonBackground", "Accent"), ("AccentButtonBackgroundPointerOver", "Accent"),
        ("AccentButtonBackgroundPressed", "Accent"), ("AccentButtonBorderBrush", "Accent"),
        ("AccentButtonBorderBrushPointerOver", "AccentText"), ("AccentButtonBorderBrushPressed", "Accent"),
        ("AccentButtonForeground", "OnAccent"), ("AccentButtonForegroundPointerOver", "OnAccent"),
        ("AccentButtonForegroundPressed", "OnAccent"),
        ("ButtonBorderBrush", "BorderStrong"), ("ButtonBorderBrushPointerOver", "FocusGlow"), ("ButtonBorderBrushPressed", "BorderStrong"),
        ("TabItemHeaderForegroundSelected", "AccentText"), ("TabItemHeaderForegroundSelectedPointerOver", "AccentText"),
        ("TabItemHeaderForegroundSelectedPressed", "AccentText"), ("TabItemHeaderSelectedPipeFill", "Emphasis"),
    ];

    // <Token>SoftBrush: a token at low opacity, for card frames and the tint behind a callout.
    private static readonly (string Token, byte Alpha)[] _softBrushes =
        [("Border", 0x99), ("Warning", 0x1F), ("DangerText", 0x1F), ("AccentText", 0x1F)];

    public static void Complete(ResourceDictionary theme)
    {
        foreach (var token in ThemeTokens.Colors)
        {
            theme[ThemeTokens.BrushKey(token)] = new SolidColorBrush(ColorOf(theme, token));
        }

        foreach (var (token, alpha) in _softBrushes)
        {
            theme[token + "SoftBrush"] = new SolidColorBrush(WithAlpha(ColorOf(theme, token), alpha));
        }

        AddDerivedBrushes(theme);
        foreach (var (fluentKey, token) in _fluentBrushes)
        {
            theme[fluentKey] = theme[ThemeTokens.BrushKey(token)];
        }
    }

    /// <summary>A missing token is a bug in a theme file, which the theme tests catch, not a user error.</summary>
    public static Color ColorOf(ResourceDictionary theme, string token) =>
        theme.TryGetValue(ThemeTokens.ColorKey(token), out var value) && value is Color color
            ? color
            : throw new InvalidOperationException($"The theme does not define {ThemeTokens.ColorKey(token)}.");

    private static void AddDerivedBrushes(ResourceDictionary theme)
    {
        var border = ColorOf(theme, "Border");
        theme[ThemeTokens.EmphasisBrush] = Horizontal(ColorOf(theme, "EmphasisStart"), ColorOf(theme, "EmphasisEnd"));
        theme["BorderFadeInBrush"] = Horizontal(WithAlpha(border, 0), border);
        theme["BorderFadeOutBrush"] = Horizontal(border, WithAlpha(border, 0));
        theme["VeilBrush"] = new SolidColorBrush(WithAlpha(ColorOf(theme, "Background"), 0xBF));

        var smoke = ColorOf(theme, "EmphasisStart");
        theme["SmokeBrush"] = new RadialGradientBrush
        {
            Center = new RelativePoint(0.18, 0, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0.18, 0, RelativeUnit.Relative),
            RadiusX = new RelativeScalar(0.6, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(0.5, RelativeUnit.Relative),
            GradientStops = { new GradientStop(WithAlpha(smoke, 0x2E), 0), new GradientStop(WithAlpha(smoke, 0), 1) },
        };

        var glow = ColorOf(theme, "FocusGlow");
        theme[ThemeTokens.GlowShadow] = new BoxShadows(new BoxShadow { Blur = GlowRadiusOf(theme), Color = WithAlpha(glow, 0x99) });
    }

    private static LinearGradientBrush Horizontal(Color start, Color end) => new()
    {
        StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
        GradientStops = { new GradientStop(start, 0), new GradientStop(end, 1) },
    };

    private static Color WithAlpha(Color color, byte alpha) => Color.FromArgb(alpha, color.R, color.G, color.B);

    private static double GlowRadiusOf(ResourceDictionary theme) =>
        theme.TryGetValue(ThemeTokens.GlowRadius, out var value) && value is double radius
            ? radius
            : throw new InvalidOperationException($"The theme does not define {ThemeTokens.GlowRadius}.");
}
