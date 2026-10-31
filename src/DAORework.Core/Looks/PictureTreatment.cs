namespace DAORework.Core.Looks;

/// <summary>
/// What a background picture looks like once it is blended into a look (ADR-0017), and so how much veil it needs.
/// The tint maps the picture's brightness onto the theme, from its Background (dark) to its Accent (bright).
/// </summary>
public static class PictureTreatment
{
    /// <summary>What <paramref name="pixel"/> becomes at <paramref name="tint"/> percent towards the theme.</summary>
    public static LookColor Tinted(LookColor pixel, LookColor background, LookColor accent, int tint)
    {
        var toned = Contrast.Over(background, accent, pixel.Luminance);
        return Contrast.Over(pixel, toned, Math.Clamp(tint, 0, 100) / 100d);
    }

    /// <summary>
    /// Tints a whole picture in place, as <see cref="Tinted"/> does one pixel. <paramref name="rgba"/> holds 4 bytes a
    /// pixel (red, green, blue, alpha; alpha is kept). The luminance uses a table, so a 3840 px picture takes a moment.
    /// </summary>
    public static void TintRgba(Span<byte> rgba, LookColor background, LookColor accent, int tint)
    {
        var a = Math.Clamp(tint, 0, 100) / 100d;
        if (a == 0)
        {
            return;
        }

        Span<double> linear = stackalloc double[256];
        for (var i = 0; i < 256; i++)
        {
            linear[i] = new LookColor((byte)i, 0, 0).Luminance / 0.2126;
        }

        for (var i = 0; i + 3 < rgba.Length; i += 4)
        {
            var lum = (0.2126 * linear[rgba[i]]) + (0.7152 * linear[rgba[i + 1]]) + (0.0722 * linear[rgba[i + 2]]);
            rgba[i] = Mix(rgba[i], background.R, accent.R, lum, a);
            rgba[i + 1] = Mix(rgba[i + 1], background.G, accent.G, lum, a);
            rgba[i + 2] = Mix(rgba[i + 2], background.B, accent.B, lum, a);
        }
    }

    /// <summary>
    /// The lowest dim, in percent, at which every text token passes over the tinted picture's brightest part. Blur only
    /// softens highlights, so ignoring it keeps the floor on the safe side.
    /// </summary>
    public static int DimFloor(LookColor brightest, LookColor background, LookColor accent, int tint, IReadOnlyCollection<TextRequirement> text)
    {
        var lowest = VeilFloor.Lowest(background, Tinted(brightest, background, accent, tint), text);
        return (int)Math.Ceiling(Math.Round(lowest * 100, 6));
    }

    private static byte Mix(byte channel, byte dark, byte bright, double lum, double amount)
    {
        var toned = dark + ((bright - dark) * lum);
        return (byte)Math.Round(channel + ((toned - channel) * amount));
    }
}
