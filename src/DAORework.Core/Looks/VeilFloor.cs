namespace DAORework.Core.Looks;

/// <summary>
/// The lowest veil over a background picture at which every text token stays readable (ADR-0017). The importer measures
/// the treated picture's brightest part; the veil is the base theme's Background painted over it at some opacity.
/// </summary>
public static class VeilFloor
{
    /// <returns>The lowest opacity, 0 to 1 in hundredths, at which every requirement passes over the brightest part.</returns>
    public static double Lowest(LookColor veil, LookColor brightest, IReadOnlyCollection<TextRequirement> text)
    {
        ArgumentNullException.ThrowIfNull(text);
        for (var percent = 0; percent < 100; percent++)
        {
            var seen = Contrast.Over(brightest, veil, percent / 100d);
            if (text.All(requirement => Contrast.Ratio(requirement.Color, seen) >= requirement.Needed))
            {
                return percent / 100d;
            }
        }

        return 1;
    }
}
