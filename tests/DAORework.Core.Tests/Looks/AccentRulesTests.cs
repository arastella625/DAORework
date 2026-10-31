using DAORework.Core.Diagnostics;
using DAORework.Core.Looks;
using Xunit;

namespace DAORework.Core.Tests.Looks;

/// <summary>ADR-0017: any pick either becomes an accent that passes AA, or is refused because red means danger.</summary>
public sealed class AccentRulesTests
{
    // Witch of the Wilds: the label on the accent, and the page, a panel and a card.
    private static readonly AccentSurfaces _wilds = new(
        Hex("#E8DFCF"), [Hex("#120D18"), Hex("#1E1528"), Hex("#2A1E38")]);

    [Fact]
    public void EveryHue_PassesOrIsRefusedAsRed()
    {
        var failures = new List<string>();
        for (var hue = 0; hue < 360; hue++)
        {
            foreach (var lightness in new[] { 0.15, 0.5, 0.85 })
            {
                var pick = LookColor.FromHsl(new Hsl(hue, 0.8, lightness));
                var derived = AccentRules.Derive(pick, _wilds);
                if (AccentRules.IsRed(pick))
                {
                    if (derived.IsSuccess || derived.Error!.Code != DiagnosticCodes.LookAccentReserved)
                    {
                        failures.Add($"{pick} at {hue}° is red but was not refused");
                    }

                    continue;
                }

                if (!derived.TryGetValue(out var palette, out var error))
                {
                    failures.Add($"{pick} at {hue}°: {error.Code}");
                    continue;
                }

                failures.AddRange(Check(pick, palette));
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures.Take(20)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(345)]
    [InlineData(15)]
    public void Red_IsRefused(double hue)
    {
        var derived = AccentRules.Derive(LookColor.FromHsl(new Hsl(hue, 0.7, 0.45)), _wilds);

        Assert.Equal(DiagnosticCodes.LookAccentReserved, derived.Error!.Code);
    }

    [Fact]
    public void AGrey_IsNotARed()
    {
        Assert.True(AccentRules.Derive(Hex("#808080"), _wilds).IsSuccess);
    }

    [Fact]
    public void APickThatAlreadyPasses_IsKeptAsTheFill_AndOnlyTheTextIsLightened()
    {
        var palette = AccentRules.Derive(Hex("#6D45C4"), _wilds).Value; // The default theme's own accent.

        Assert.Equal(Hex("#6D45C4"), palette.Accent);
        Assert.False(palette.Darkened);
        Assert.True(palette.Lightened);
    }

    [Fact]
    public void TheHueIsKept()
    {
        var pick = Hex("#3BA3D9");
        var palette = AccentRules.Derive(pick, _wilds).Value;

        // Within a degree: each channel is rounded to a byte.
        Assert.InRange(palette.Accent.ToHsl().Hue, pick.ToHsl().Hue - 1, pick.ToHsl().Hue + 1);
        Assert.InRange(palette.AccentText.ToHsl().Hue, pick.ToHsl().Hue - 1, pick.ToHsl().Hue + 1);
    }

    private static IEnumerable<string> Check(LookColor pick, AccentPalette palette)
    {
        if (Contrast.Ratio(_wilds.OnAccent, palette.Accent) < Contrast.Text)
        {
            yield return $"{pick}: label on {palette.Accent} fails";
        }

        foreach (var surface in _wilds.Surfaces)
        {
            if (Contrast.Ratio(palette.AccentText, surface) < Contrast.Text)
            {
                yield return $"{pick}: text {palette.AccentText} on {surface} fails";
            }

            if (Contrast.Ratio(palette.FocusGlow, surface) < Contrast.LargeOrUi)
            {
                yield return $"{pick}: glow {palette.FocusGlow} on {surface} fails";
            }
        }
    }

    private static LookColor Hex(string hex) => LookColor.Parse(hex).Value;
}
