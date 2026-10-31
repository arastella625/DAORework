using DAORework.Core.Looks;
using SkiaSharp;

namespace DAORework.Infrastructure.Looks;

/// <summary>
/// Finds a picture's brightest part for the veil floor (ADR-0017): the pixel at the 98th percentile of luminance, so a
/// few specks of white do not force a full veil, and a bright sky does. Large pictures are sampled on a grid.
/// </summary>
internal static class PictureBrightness
{
    private const double Percentile = 0.98;
    private const int MaxSamples = 250_000;

    public static LookColor Brightest(SKBitmap picture)
    {
        var step = Math.Max(1, (int)Math.Ceiling(Math.Sqrt((double)picture.Width * picture.Height / MaxSamples)));
        var samples = new List<LookColor>();
        for (var y = 0; y < picture.Height; y += step)
        {
            for (var x = 0; x < picture.Width; x += step)
            {
                var pixel = picture.GetPixel(x, y);
                samples.Add(new LookColor(pixel.Red, pixel.Green, pixel.Blue));
            }
        }

        samples.Sort((a, b) => a.Luminance.CompareTo(b.Luminance));
        return samples[(int)Math.Floor((samples.Count - 1) * Percentile)];
    }
}
