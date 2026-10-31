namespace DAORework.Core.Looks;

/// <summary>
/// How a look's picture is drawn (ADR-0017): its file in the look's folder, how it fills the window, and the tint, veil
/// and blur that blend it into the theme. The veil never goes below the floor <see cref="PictureTreatment"/> computes
/// from the picture's measured <see cref="Brightest"/> color, the base theme and the tint.
/// </summary>
public sealed record LookBackground
{
    public const int MaxBlur = 24;

    public required string FileName { get; init; }

    public BackgroundFit Fit { get; init; } = BackgroundFit.Fill;

    /// <summary>Gets how far the picture is mapped onto the theme's colors, 0 to 100.</summary>
    public int Tint { get; init; } = 40;

    /// <summary>Gets the veil of the theme's background over the picture, 0 to 100.</summary>
    public int Dim { get; init; } = 80;

    /// <summary>Gets the blur, in pixels, 0 to <see cref="MaxBlur"/>.</summary>
    public int Blur { get; init; } = 6;

    /// <summary>Gets the picture's brightest part (its 98th percentile by luminance) as <c>#RRGGBB</c>, measured on import.</summary>
    public required string Brightest { get; init; }
}
