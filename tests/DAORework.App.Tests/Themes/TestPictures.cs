using SkiaSharp;

namespace DAORework.App.Tests.Themes;

/// <summary>Pictures the tests draw, so no picture file is kept in the repository.</summary>
internal static class TestPictures
{
    /// <summary>A dusk sky over dark water, with a pale moon: the brightest part is near white.</summary>
    public static byte[] Dusk()
    {
        using var bitmap = new SKBitmap(640, 400);
        using (var canvas = new SKCanvas(bitmap))
        using (var sky = new SKPaint())
        {
            sky.Shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, 260), [new SKColor(0x1C, 0x12, 0x30), new SKColor(0x5B, 0x2F, 0x5E), new SKColor(0xC9, 0x7A, 0x55)], SKShaderTileMode.Clamp);
            canvas.DrawRect(0, 0, 640, 260, sky);
            using var water = new SKPaint { Color = new SKColor(0x13, 0x0D, 0x1C) };
            canvas.DrawRect(0, 260, 640, 140, water);
            using var moon = new SKPaint { Color = new SKColor(0xF5, 0xE6, 0xC8), IsAntialias = true };
            canvas.DrawCircle(450, 90, 28, moon);
        }

        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
