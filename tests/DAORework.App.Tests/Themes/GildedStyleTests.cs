using Avalonia.Headless.XUnit;
using DAORework.App.Themes;
using Xunit;

namespace DAORework.App.Tests.Themes;

/// <summary>The gilded-tome style (ADR-0026 §4) is drawn from each theme's own tokens; a plain theme draws no ornaments.</summary>
public sealed class GildedStyleTests
{
    [AvaloniaTheory]
    [MemberData(nameof(ThemeDictionaries.Ids), MemberType = typeof(ThemeDictionaries))]
    public void EveryTheme_HasTheGildedBrushes_AndOnlyAPlainOneDropsTheOrnaments(string id)
    {
        var theme = ThemeDictionaries.ById(id);
        var dictionary = ThemeDictionaries.Of(theme);

        foreach (var key in new[] { "CardBackgroundBrush", "InnerRuleBrush", "ButtonRuleShadow", "GemOnBrush", "GemOffBrush" })
        {
            Assert.True(dictionary.ContainsKey(key), $"{id} has no {key}.");
        }

        var plain = (double)dictionary[ThemeTokens.GlowRadius]! == 0; // Korcari: no glow, so no ornaments either.
        Assert.Equal(plain ? 0d : 1d, (double)dictionary["OrnamentOpacity"]!);
    }
}
