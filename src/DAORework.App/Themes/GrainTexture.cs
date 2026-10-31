using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace DAORework.App.Themes;

/// <summary>
/// The vellum grain behind every page (ADR-0016 rule 5): a small tile of grey noise the app makes itself, so nothing is
/// shipped and nothing is taken from the game. The noise is seeded, so the screenshots stay the same between runs.
/// </summary>
internal static class GrainTexture
{
    private const int Size = 96;
    private const int Seed = 1709;

    public static ImageBrush CreateBrush()
    {
        var bitmap = new WriteableBitmap(new PixelSize(Size, Size), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
        var random = new Random(Seed);
        var row = new byte[Size * 4];
        using (var buffer = bitmap.Lock())
        {
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < row.Length; x += 4)
                {
                    var grey = (byte)random.Next(256);
                    (row[x], row[x + 1], row[x + 2], row[x + 3]) = (grey, grey, grey, 0xFF);
                }

                Marshal.Copy(row, 0, buffer.Address + (y * buffer.RowBytes), row.Length);
            }
        }

        return new ImageBrush(bitmap)
        {
            TileMode = TileMode.Tile,
            Stretch = Stretch.None,
            DestinationRect = new RelativeRect(0, 0, Size, Size, RelativeUnit.Absolute),
        };
    }
}
