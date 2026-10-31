using System.Globalization;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Core.Looks;

/// <summary>An opaque sRGB color, as a look stores it: <c>#RRGGBB</c> (ADR-0017).</summary>
public readonly record struct LookColor(byte R, byte G, byte B)
{
    /// <summary>Gets the WCAG 2.x relative luminance, 0 for black to 1 for white.</summary>
    public double Luminance => (0.2126 * Linear(R)) + (0.7152 * Linear(G)) + (0.0722 * Linear(B));

    public static Result<LookColor> Parse(string? text)
    {
        var hex = text?.Trim().TrimStart('#') ?? string.Empty;
        return hex.Length == 6 && int.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value)
            ? Result.Success(new LookColor((byte)(value >> 16), (byte)(value >> 8), (byte)value))
            : Result.Failure<LookColor>(new Error(DiagnosticCodes.LookColorInvalid, $"'{text}' is not a color like #8B5CF6."));
    }

    public static LookColor FromHsl(Hsl hsl)
    {
        var (h, s, l) = (hsl.Hue / 360, hsl.Saturation, hsl.Lightness);
        if (s == 0)
        {
            var grey = ToByte(l);
            return new LookColor(grey, grey, grey);
        }

        var q = l < 0.5 ? l * (1 + s) : l + s - (l * s);
        var p = (2 * l) - q;
        return new LookColor(ToByte(Channel(p, q, h + (1d / 3))), ToByte(Channel(p, q, h)), ToByte(Channel(p, q, h - (1d / 3))));
    }

    public Hsl ToHsl()
    {
        var (r, g, b) = (R / 255d, G / 255d, B / 255d);
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var l = (max + min) / 2;
        var delta = max - min;
        if (delta == 0)
        {
            return new Hsl(0, 0, l);
        }

        var s = l > 0.5 ? delta / (2 - max - min) : delta / (max + min);
        double h;
        if (max == r)
        {
            h = ((g - b) / delta) + (g < b ? 6 : 0);
        }
        else if (max == g)
        {
            h = ((b - r) / delta) + 2;
        }
        else
        {
            h = ((r - g) / delta) + 4;
        }

        return new Hsl(h * 60, s, l);
    }

    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";

    private static double Linear(byte channel)
    {
        var c = channel / 255d;
        return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }

    private static double Channel(double p, double q, double t)
    {
        t = t < 0 ? t + 1 : t;
        t = t > 1 ? t - 1 : t;
        if (t < 1d / 6)
        {
            return p + ((q - p) * 6 * t);
        }

        if (t < 0.5)
        {
            return q;
        }

        return t < 2d / 3 ? p + ((q - p) * ((2d / 3) - t) * 6) : p;
    }

    private static byte ToByte(double value) => (byte)Math.Round(Math.Clamp(value, 0, 1) * 255);
}
