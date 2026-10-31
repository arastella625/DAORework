using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Core.Looks;

/// <summary>
/// Turns a user's accent pick into tokens that pass ADR-0016's contrast rule, by keeping its hue and saturation and
/// changing only lightness (ADR-0017). A red is refused: red means danger (ADR-0016 rule 3).
/// </summary>
public static class AccentRules
{
    /// <summary>Hues from here round to <see cref="RedEnd"/> are red.</summary>
    public const double RedStart = 330;

    public const double RedEnd = 20;

    /// <summary>Below this saturation a color is a grey and has no hue to speak of, so it is never red.</summary>
    public const double GreySaturation = 0.2;

    private const double Step = 0.01;

    public static bool IsRed(LookColor color)
    {
        var hsl = color.ToHsl();
        return hsl.Saturation >= GreySaturation && (hsl.Hue >= RedStart || hsl.Hue <= RedEnd);
    }

    /// <returns>The pick, or <c>DAO_LOOK_ACCENT_RESERVED</c> when it is a red.</returns>
    public static Result<LookColor> Allowed(LookColor pick) =>
        IsRed(pick)
            ? Result.Failure<LookColor>(new Error(
                DiagnosticCodes.LookAccentReserved,
                $"{pick} is a red, and red is kept for danger.",
                "Pick another hue; the danger color stays red in every look."))
            : Result.Success(pick);

    public static Result<AccentPalette> Derive(LookColor pick, AccentSurfaces surfaces)
    {
        ArgumentNullException.ThrowIfNull(surfaces);
        if (!Allowed(pick).TryGetValue(out _, out var reserved))
        {
            return Result.Failure<AccentPalette>(reserved);
        }

        var hsl = pick.ToHsl();
        var fill = Find(hsl, -Step, color => Contrast.Ratio(surfaces.OnAccent, color) >= Contrast.Text);
        var text = Find(hsl, Step, color => surfaces.Surfaces.All(surface => Contrast.Ratio(color, surface) >= Contrast.Text));
        if (fill is null || text is null)
        {
            return Result.Failure<AccentPalette>(new Error(
                DiagnosticCodes.LookAccentUnreadable,
                $"No lightness of {pick} gives readable text on this look's base theme.",
                "Pick another color or another base theme."));
        }

        // The focus glow is a step lighter than accent text, so it reads as a halo and clears 3:1 wherever text clears 4.5:1.
        var textHsl = text.Value.ToHsl();
        var glow = LookColor.FromHsl(textHsl with { Lightness = Math.Min(textHsl.Lightness + 0.1, 0.95) });
        return Result.Success(new AccentPalette(fill.Value, text.Value, glow, fill.Value != pick, text.Value != pick));
    }

    // Walks the lightness from the pick in one direction until the color passes, or returns null at the end of the range.
    private static LookColor? Find(Hsl start, double step, Func<LookColor, bool> passes)
    {
        for (var lightness = start.Lightness; lightness is >= 0 and <= 1; lightness += step)
        {
            var color = LookColor.FromHsl(start with { Lightness = lightness });
            if (passes(color))
            {
                return color;
            }
        }

        return null;
    }
}
