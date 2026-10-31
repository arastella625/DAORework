using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DAORework.Core.Looks;
using SkiaSharp;

namespace DAORework.App.Themes;

/// <summary>
/// Turns a look's picture into what the window shows (ADR-0017): decoded from the bytes the look store read, then tinted
/// once, here, rather than on every frame. The importer already checked and scaled the picture.
/// </summary>
internal static class LookPictureRenderer
{
    /// <exception cref="InvalidDataException">The bytes are not a picture (the look's copy was damaged after import).</exception>
    public static WriteableBitmap Render(byte[] picture, LookColor background, LookColor accent, int tint)
    {
        using var data = SKData.CreateCopy(picture);
        using var codec = SKCodec.Create(data) ?? throw new InvalidDataException("The look's picture is not a picture.");
        using var decoded = SKBitmap.Decode(codec, codec.Info.WithColorType(SKColorType.Rgba8888).WithAlphaType(SKAlphaType.Unpremul))
            ?? throw new InvalidDataException("The look's picture could not be decoded.");
        var pixels = decoded.Bytes;
        PictureTreatment.TintRgba(pixels, background, accent, tint);

        var bitmap = new WriteableBitmap(new PixelSize(decoded.Width, decoded.Height), new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Unpremul);
        using var buffer = bitmap.Lock();
        var row = decoded.Width * 4;
        for (var y = 0; y < decoded.Height; y++)
        {
            Marshal.Copy(pixels, y * decoded.RowBytes, buffer.Address + (y * buffer.RowBytes), row);
        }

        return bitmap;
    }
}
