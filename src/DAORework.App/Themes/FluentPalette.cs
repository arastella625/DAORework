using Avalonia.Controls;
using Avalonia.Themes.Fluent;

namespace DAORework.App.Themes;

/// <summary>
/// Fills Fluent's dark palette from a theme's tokens, so every Fluent control (tabs, check boxes, combo boxes, text
/// boxes) takes the theme's colors without a style of its own. Fluent only accepts Light and Dark palettes, and every
/// DAORework theme inherits Dark, so the one palette is refilled when the theme changes.
/// </summary>
internal static class FluentPalette
{
    public static void Fill(ColorPaletteResources palette, ResourceDictionary theme)
    {
        var background = ThemeResources.ColorOf(theme, "Background");
        var surface = ThemeResources.ColorOf(theme, "Surface");
        var raised = ThemeResources.ColorOf(theme, "SurfaceRaised");
        var text = ThemeResources.ColorOf(theme, "TextPrimary");
        var secondary = ThemeResources.ColorOf(theme, "TextSecondary");

        palette.Accent = ThemeResources.ColorOf(theme, "Accent");
        palette.RegionColor = background;
        palette.AltHigh = background;
        palette.AltMediumHigh = background;
        palette.AltMedium = background;
        palette.AltMediumLow = background;
        palette.AltLow = background;
        palette.BaseHigh = text;
        palette.BaseMediumHigh = text;
        palette.BaseMedium = secondary;
        palette.BaseMediumLow = secondary;
        palette.BaseLow = raised;
        palette.ChromeLow = surface;
        palette.ChromeMediumLow = surface;
        palette.ChromeMedium = raised;
        palette.ChromeHigh = ThemeResources.ColorOf(theme, "BorderStrong");
        palette.ChromeAltLow = text;
        palette.ChromeWhite = text;
        palette.ChromeGray = secondary;
        palette.ChromeDisabledHigh = ThemeResources.ColorOf(theme, "Border");
        palette.ChromeDisabledLow = secondary;
        palette.ChromeBlackHigh = background;
        palette.ChromeBlackMedium = background;
        palette.ChromeBlackMediumLow = background;
        palette.ChromeBlackLow = background;
        palette.ListLow = surface;
        palette.ListMedium = raised;
        palette.ErrorText = ThemeResources.ColorOf(theme, "DangerText");
    }
}
