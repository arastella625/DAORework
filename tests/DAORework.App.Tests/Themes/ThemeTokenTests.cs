using Avalonia.Headless.XUnit;
using Avalonia.Media;
using DAORework.App.Themes;
using Xunit;

namespace DAORework.App.Tests.Themes;

/// <summary>Every theme defines every token, and red stays reserved for danger (ADR-0016).</summary>
public sealed class ThemeTokenTests
{
    [Fact]
    public void BuiltInThemes_AreTheFourOfTheAdr_WithWitchOfTheWildsAsTheDefault()
    {
        Assert.Equal(["witch-of-the-wilds", "korcari", "the-fade", "blood-and-shadow"], AppThemes.All.Select(theme => theme.Id));
        Assert.Same(AppThemes.WitchOfTheWilds, AppThemes.Default);
        Assert.Equal(AppThemes.All.Count, AppThemes.All.Select(theme => theme.Variant.Key).Distinct().Count());
    }

    [AvaloniaTheory]
    [MemberData(nameof(ThemeDictionaries.Ids), MemberType = typeof(ThemeDictionaries))]
    public void Theme_DefinesEveryColorBrushAndTheGlow(string id)
    {
        var dictionary = ThemeDictionaries.Of(ThemeDictionaries.ById(id));

        foreach (var token in ThemeTokens.Colors)
        {
            Assert.IsType<Color>(dictionary[ThemeTokens.ColorKey(token)]);
            Assert.IsType<SolidColorBrush>(dictionary[ThemeTokens.BrushKey(token)]);
        }

        Assert.IsType<double>(dictionary[ThemeTokens.GlowRadius]);
        Assert.IsType<BoxShadows>(dictionary[ThemeTokens.GlowShadow]);
        Assert.IsType<LinearGradientBrush>(dictionary[ThemeTokens.EmphasisBrush]);
    }

    [AvaloniaTheory]
    [MemberData(nameof(ThemeDictionaries.Ids), MemberType = typeof(ThemeDictionaries))]
    public void Accent_IsNeverARed(string id)
    {
        var hue = Contrast.Hue(ThemeDictionaries.ColorOf(ThemeDictionaries.ById(id), "Accent"));

        Assert.True(hue is > 20 and < 330, $"{id}'s accent hue is {hue:F0}°, in the red range kept for danger.");
    }

    [AvaloniaFact]
    public void Korcari_HasNoGlow()
    {
        Assert.Equal(0d, ThemeDictionaries.Of(AppThemes.Korcari)[ThemeTokens.GlowRadius]);
    }
}
