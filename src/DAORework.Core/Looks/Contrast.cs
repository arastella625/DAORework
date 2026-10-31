namespace DAORework.Core.Looks;

/// <summary>WCAG 2.x contrast and alpha blending, for the rules every theme and look follows (ADR-0016 rule 2).</summary>
public static class Contrast
{
    /// <summary>4.5:1, the AA minimum for body text.</summary>
    public const double Text = 4.5;

    /// <summary>3:1, the AA minimum for large text (headings), control outlines and the focus ring.</summary>
    public const double LargeOrUi = 3;

    public static double Ratio(LookColor first, LookColor second)
    {
        var (a, b) = (first.Luminance, second.Luminance);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    /// <summary>What <paramref name="above"/> looks like painted over <paramref name="below"/> at <paramref name="opacity"/> (0 to 1).</summary>
    public static LookColor Over(LookColor below, LookColor above, double opacity)
    {
        var a = Math.Clamp(opacity, 0, 1);
        byte Mix(byte under, byte over) => (byte)Math.Round((over * a) + (under * (1 - a)));
        return new LookColor(Mix(below.R, above.R), Mix(below.G, above.G), Mix(below.B, above.B));
    }
}
