using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Xunit;

namespace DAORework.App.Tests.Themes;

/// <summary>ADR-0016 rule 2: WCAG AA in every theme. 4.5:1 for text, 3:1 for headings, control outlines and focus.</summary>
public sealed class ThemeContrastTests
{
    private const double Text = 4.5;
    private const double LargeOrUi = 3;

    private static readonly string[] _surfaces = ["Background", "Surface", "SurfaceRaised"];

    // Text that can sit on any surface: the page, a panel or a card.
    private static readonly string[] _textTokens =
        ["TextPrimary", "TextSecondary", "AccentText", "AccentSecondaryText", "Success", "Warning", "DangerText"];

    [AvaloniaTheory]
    [MemberData(nameof(ThemeDictionaries.Ids), MemberType = typeof(ThemeDictionaries))]
    public void EveryPair_PassesAa(string id)
    {
        var theme = ThemeDictionaries.ById(id);
        var failures = new List<string>();

        void Check(string foreground, string background, double needed)
        {
            var ratio = Contrast.Ratio(ThemeDictionaries.ColorOf(theme, foreground), ThemeDictionaries.ColorOf(theme, background));
            if (ratio < needed)
            {
                failures.Add($"{foreground} on {background}: {ratio:F2}:1, needs {needed}:1");
            }
        }

        foreach (var surface in _surfaces)
        {
            foreach (var token in _textTokens)
            {
                Check(token, surface, Text);
            }

            Check("Heading", surface, LargeOrUi);
        }

        Check("OnAccent", "Accent", Text);
        Check("TextPrimary", "Danger", Text);
        Check("BorderStrong", "Surface", LargeOrUi);
        Check("BorderStrong", "SurfaceRaised", LargeOrUi);
        Check("FocusGlow", "Surface", LargeOrUi);
        Check("FocusGlow", "SurfaceRaised", LargeOrUi);

        Assert.True(failures.Count == 0, $"{id}: {string.Join("; ", failures)}");
    }

    /// <summary>Text on the derived backgrounds: a callout's tint, and the backdrop where its smoke is strongest.</summary>
    [AvaloniaTheory]
    [MemberData(nameof(ThemeDictionaries.Ids), MemberType = typeof(ThemeDictionaries))]
    public void TextOnTintsAndTheBackdrop_PassesAa(string id)
    {
        var theme = ThemeDictionaries.ById(id);
        var dictionary = ThemeDictionaries.Of(theme);
        Color Token(string token) => ThemeDictionaries.ColorOf(theme, token);
        Color SoftOverRaised(string key) => Contrast.Over(Token("SurfaceRaised"), ((ISolidColorBrush)dictionary[key]!).Color);

        // The smoke at its center, then the grain at its brightest: a white pixel at the Backdrop's 4% (Controls.axaml).
        var smoke = ((RadialGradientBrush)dictionary["SmokeBrush"]!).GradientStops[0].Color;
        var backdrop = Contrast.Over(Contrast.Over(Token("Background"), smoke), Colors.White, 0.04);
        var failures = new List<string>();
        void Check(string label, Color foreground, Color background, double needed)
        {
            var ratio = Contrast.Ratio(foreground, background);
            if (ratio < needed)
            {
                failures.Add($"{label}: {ratio:F2}:1, needs {needed}:1");
            }
        }

        foreach (var token in _textTokens)
        {
            Check($"{token} on the backdrop", Token(token), backdrop, Text);
        }

        Check("Heading on the backdrop", Token("Heading"), backdrop, LargeOrUi);
        foreach (var (soft, label) in new[] { ("WarningSoftBrush", "Warning"), ("DangerTextSoftBrush", "DangerText") })
        {
            Check($"TextPrimary on the {label} callout", Token("TextPrimary"), SoftOverRaised(soft), Text);
            Check($"{label} icon on its callout", Token(label), SoftOverRaised(soft), LargeOrUi); // Callout text is TextPrimary.
        }

        Assert.True(failures.Count == 0, $"{id}: {string.Join("; ", failures)}");
    }
}
