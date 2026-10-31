namespace DAORework.App.Themes;

/// <summary>
/// The color tokens every theme defines (ADR-0016). Each is a <c>&lt;Name&gt;Color</c> in the theme's file and a
/// <c>&lt;Name&gt;Brush</c> that <see cref="ThemeResources"/> adds. Views use the brushes, never a color literal.
/// </summary>
public static class ThemeTokens
{
    /// <summary>The resource key of the glow's blur radius, in pixels. Zero turns the glow off.</summary>
    public const string GlowRadius = nameof(GlowRadius);

    /// <summary>The resource key of the rare violet-to-crimson (or theme's own) gradient.</summary>
    public const string EmphasisBrush = nameof(EmphasisBrush);

    /// <summary>The resource key of the box shadow used for hover and focus.</summary>
    public const string GlowShadow = nameof(GlowShadow);

    public static IReadOnlyList<string> Colors { get; } =
    [
        "Background", "Surface", "SurfaceRaised", "Border", "BorderStrong", "TextPrimary", "TextSecondary", "Heading",
        "Accent", "OnAccent", "AccentText", "AccentSecondary", "AccentSecondaryText", "Success", "Warning", "Danger",
        "DangerText", "FocusGlow", "EmphasisStart", "EmphasisEnd",
    ];

    public static string ColorKey(string token) => token + "Color";

    public static string BrushKey(string token) => token + "Brush";
}
