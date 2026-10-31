using System.Buffers.Binary;
using System.IO.Abstractions;
using System.Security.Cryptography;
using DAORework.Application.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Looks;
using DAORework.Infrastructure.Looks;
using Microsoft.Extensions.Options;
using SkiaSharp;
using Xunit;

namespace DAORework.Infrastructure.Tests.Looks;

/// <summary>The background picture importer (ADR-0017) against a real temp folder, with pictures the tests draw.</summary>
public sealed class SkiaBackgroundImageImporterTests : IDisposable
{
    private const string LookId = "look-test1";

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "DAORework.Tests", Guid.NewGuid().ToString("N"));

    public SkiaBackgroundImageImporterTests() => Directory.CreateDirectory(_directory);

    private string Copy => Path.Combine(_directory, "looks", LookId, SkiaBackgroundImageImporter.FileName);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public async Task Import_CopiesThePicture_AndMeasuresItsBrightestPart()
    {
        var source = Draw("white.png", 10, 10, SKEncodedImageFormat.Png, (_, _) => SKColors.White);
        var before = SHA256.HashData(File.ReadAllBytes(source));

        var imported = await CreateImporter().ImportAsync(source, LookId);

        Assert.Equal(new LookColor(255, 255, 255), imported.Value.Brightest);
        Assert.Equal((10, 10), (imported.Value.Width, imported.Value.Height));
        using var copy = SKBitmap.Decode(Copy);
        Assert.Equal(10, copy.Width);
        Assert.Equal(before, SHA256.HashData(File.ReadAllBytes(source))); // The picked file is only read.
    }

    [Fact]
    public async Task Import_ScalesALargePictureTo3840OnItsLongEdge()
    {
        var source = Draw("wide.jpg", 5000, 100, SKEncodedImageFormat.Jpeg, (x, _) => new SKColor((byte)(x * 255 / 4999), 64, 32));

        var imported = await CreateImporter().ImportAsync(source, LookId);

        Assert.Equal((3840, 77), (imported.Value.Width, imported.Value.Height));
        using var copy = SKBitmap.Decode(Copy);
        Assert.Equal(3840, copy.Width);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(5, 255)]
    public async Task Brightest_IgnoresSpecks_ButNotABrightArea(int whiteRows, byte expected)
    {
        var source = Draw("specks.png", 100, 100, SKEncodedImageFormat.Png, (x, y) => y < whiteRows && (whiteRows > 1 || x == 0) ? SKColors.White : SKColors.Black);

        var imported = await CreateImporter().ImportAsync(source, LookId);

        Assert.Equal(new LookColor(expected, expected, expected), imported.Value.Brightest);
    }

    [Fact]
    public async Task Import_RefusesWhatIsNotAPicture_AndAFileThatIsNotThere()
    {
        var text = Path.Combine(_directory, "notes.png");
        await File.WriteAllTextAsync(text, "not a picture");

        var notAPicture = await CreateImporter().ImportAsync(text, LookId);
        var missing = await CreateImporter().ImportAsync(Path.Combine(_directory, "missing.png"), LookId);

        Assert.Equal(DiagnosticCodes.LookImageUnreadable, notAPicture.Error!.Code);
        Assert.Equal(DiagnosticCodes.LookImageUnreadable, missing.Error!.Code);
        Assert.False(File.Exists(Copy));
    }

    [Fact]
    public async Task Import_RefusesAFileOver50Megabytes_WithoutReadingIt()
    {
        var big = Path.Combine(_directory, "big.png");
        using (var stream = File.Create(big))
        {
            stream.SetLength(SkiaBackgroundImageImporter.MaxBytes + 1); // Sparse: nothing is written.
        }

        var imported = await CreateImporter().ImportAsync(big, LookId);

        Assert.Equal(DiagnosticCodes.LookImageTooLarge, imported.Error!.Code);
    }

    [Fact]
    public async Task Import_RefusesAPictureOverAHundredMillionPixels_BeforeDecodingIt()
    {
        var huge = Path.Combine(_directory, "huge.png");
        await File.WriteAllBytesAsync(huge, PngHeader(20_000, 20_000));

        var imported = await CreateImporter().ImportAsync(huge, LookId);

        Assert.Equal(DiagnosticCodes.LookImageTooLarge, imported.Error!.Code);
    }

    [Fact]
    public async Task Read_ReturnsTheCopysBytes_OrSaysItIsMissing()
    {
        var importer = CreateImporter();
        var missing = await importer.ReadAsync(LookId, SkiaBackgroundImageImporter.FileName);
        await importer.ImportAsync(Draw("white.png", 4, 4, SKEncodedImageFormat.Png, (_, _) => SKColors.White), LookId);

        var read = await importer.ReadAsync(LookId, SkiaBackgroundImageImporter.FileName);

        Assert.Equal(DiagnosticCodes.LookImageUnreadable, missing.Error!.Code);
        Assert.Equal(File.ReadAllBytes(Copy), read.Value);
    }

    [Fact]
    public async Task Remove_DeletesTheCopy_AndIsQuietWhenItIsGone()
    {
        var importer = CreateImporter();
        await importer.ImportAsync(Draw("white.png", 4, 4, SKEncodedImageFormat.Png, (_, _) => SKColors.White), LookId);

        await importer.RemoveAsync(LookId, SkiaBackgroundImageImporter.FileName);
        await importer.RemoveAsync(LookId, SkiaBackgroundImageImporter.FileName);

        Assert.False(File.Exists(Copy));
    }

    [Fact]
    public async Task Import_IntoSomethingThatIsNotALookId_IsAProgrammingError()
    {
        var source = Draw("white.png", 4, 4, SKEncodedImageFormat.Png, (_, _) => SKColors.White);

        await Assert.ThrowsAsync<ArgumentException>(() => CreateImporter().ImportAsync(source, "../escape"));
    }

    private SkiaBackgroundImageImporter CreateImporter() =>
        new(new FileSystem(), Options.Create(new DaoReworkOptions { AppDataDirectory = _directory }));

    private string Draw(string name, int width, int height, SKEncodedImageFormat format, Func<int, int, SKColor> color)
    {
        using var bitmap = new SKBitmap(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                bitmap.SetPixel(x, y, color(x, y));
            }
        }

        var path = Path.Combine(_directory, name);
        using var data = bitmap.Encode(format, 95);
        File.WriteAllBytes(path, data.ToArray());
        return path;
    }

    // A PNG claiming the given size, with an empty data chunk: enough for a codec to read the size, nothing to decode.
    private static byte[] PngHeader(int width, int height)
    {
        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8; // Bit depth.
        header[9] = 6; // RGBA.
        byte[] emptyZlib = [0x78, 0x9C, 0x03, 0x00, 0x00, 0x00, 0x00, 0x01];
        return [.. signature, .. Chunk("IHDR", header), .. Chunk("IDAT", emptyZlib), .. Chunk("IEND", [])];
    }

    private static byte[] Chunk(string type, byte[] data)
    {
        var typed = new byte[4 + data.Length];
        System.Text.Encoding.ASCII.GetBytes(type).CopyTo(typed, 0);
        data.CopyTo(typed, 4);
        var chunk = new byte[4 + typed.Length + 4];
        BinaryPrimitives.WriteInt32BigEndian(chunk, data.Length);
        typed.CopyTo(chunk, 4);
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(4 + typed.Length), Crc32(typed));
        return chunk;
    }

    private static uint Crc32(byte[] bytes)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in bytes)
        {
            crc ^= b;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
        }

        return ~crc;
    }
}
