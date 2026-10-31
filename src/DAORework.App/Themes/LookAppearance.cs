using Avalonia.Controls;
using Avalonia.Media;
using DAORework.Core.Common;
using DAORework.Core.Looks;

namespace DAORework.App.Themes;

/// <summary>
/// Builds a custom look's colors (ADR-0017): its base theme's tokens, with the accent tokens derived from the look's
/// pick by <see cref="AccentRules"/> against that theme's own surfaces, so they pass the same contrast rules.
/// </summary>
public static class LookAppearance
{
    // The text that sits on the page over a picture, with the contrast each needs (ADR-0016 rule 2).
    private static readonly (string Token, double Needed)[] _text =
    [
        ("TextPrimary", Contrast.Text), ("TextSecondary", Contrast.Text), ("AccentText", Contrast.Text),
        ("AccentSecondaryText", Contrast.Text), ("Success", Contrast.Text), ("Warning", Contrast.Text),
        ("DangerText", Contrast.Text), ("Heading", Contrast.LargeOrUi),
    ];

    /// <returns>The look's completed dictionary, and the reason its accent was not used, if it was not.</returns>
    public static (ResourceDictionary Dictionary, Error? AccentProblem) Build(ResourceDictionary baseTheme, Look look)
    {
        ArgumentNullException.ThrowIfNull(baseTheme);
        ArgumentNullException.ThrowIfNull(look);
        var dictionary = new ResourceDictionary { [ThemeTokens.GlowRadius] = baseTheme[ThemeTokens.GlowRadius] };
        foreach (var token in ThemeTokens.Colors)
        {
            dictionary[ThemeTokens.ColorKey(token)] = ThemeResources.ColorOf(baseTheme, token);
        }

        var problem = look.Accent is null ? null : ApplyAccent(dictionary, look.Accent);
        ThemeResources.Complete(dictionary);
        return (dictionary, problem);
    }

    /// <summary>The look's text colors with the contrast each needs, for <see cref="PictureTreatment.DimFloor"/>.</summary>
    public static IReadOnlyCollection<TextRequirement> TextRequirements(ResourceDictionary dictionary) =>
        _text.Select(text => new TextRequirement(ToLook(dictionary, text.Token), text.Needed)).ToList();

    public static LookColor ToLook(ResourceDictionary dictionary, string token)
    {
        var color = ThemeResources.ColorOf(dictionary, token);
        return new LookColor(color.R, color.G, color.B);
    }

    // Replaces the base theme's accent tokens with ones derived from the pick, or says why the pick cannot be used.
    private static Error? ApplyAccent(ResourceDictionary dictionary, string accent)
    {
        if (!LookColor.Parse(accent).TryGetValue(out var pick, out var error))
        {
            return error;
        }

        var surfaces = new AccentSurfaces(
            ToLook(dictionary, "OnAccent"), [ToLook(dictionary, "Background"), ToLook(dictionary, "Surface"), ToLook(dictionary, "SurfaceRaised")]);
        if (!AccentRules.Derive(pick, surfaces).TryGetValue(out var palette, out error))
        {
            return error;
        }

        dictionary[ThemeTokens.ColorKey("Accent")] = ToAvalonia(palette.Accent);
        dictionary[ThemeTokens.ColorKey("AccentText")] = ToAvalonia(palette.AccentText);
        dictionary[ThemeTokens.ColorKey("FocusGlow")] = ToAvalonia(palette.FocusGlow);
        dictionary[ThemeTokens.ColorKey("EmphasisStart")] = ToAvalonia(palette.Accent);
        return null;
    }

    private static Color ToAvalonia(LookColor color) => Color.FromRgb(color.R, color.G, color.B);
}
