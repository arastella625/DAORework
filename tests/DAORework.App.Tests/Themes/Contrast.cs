using Avalonia.Media;

namespace DAORework.App.Tests.Themes;

/// <summary>WCAG 2.x contrast ratio and hue, for the theme rules in ADR-0016.</summary>
internal static class Contrast
{
    public static double Ratio(Color first, Color second)
    {
        var a = Luminance(first);
        var b = Luminance(second);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    /// <summary>What <paramref name="above"/> looks like painted over <paramref name="below"/> at its own alpha times <paramref name="opacity"/>.</summary>
    public static Color Over(Color below, Color above, double opacity = 1)
    {
        var a = above.A / 255.0 * opacity;
        byte Mix(byte under, byte over) => (byte)Math.Round((over * a) + (under * (1 - a)));
        return Color.FromRgb(Mix(below.R, above.R), Mix(below.G, above.G), Mix(below.B, above.B));
    }

    /// <returns>The hue in degrees, 0 to 360.</returns>
    public static double Hue(Color color) => color.ToHsl().H;

    private static double Luminance(Color color) =>
        (0.2126 * Channel(color.R)) + (0.7152 * Channel(color.G)) + (0.0722 * Channel(color.B));

    private static double Channel(byte value)
    {
        var c = value / 255.0;
        return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }
}
