using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Looks;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace DAORework.Infrastructure.Looks;

/// <summary>
/// Imports a background picture with SkiaSharp (ADR-0017): refuses a file over 50 MB or 100 million pixels, or one
/// that is not a PNG, JPEG or WebP; scales it to at most 3840 px on its long edge; measures its brightest part; and
/// writes the copy as <c>background.png</c> in the look's folder, atomically. The picked file is only ever read.
/// </summary>
public sealed class SkiaBackgroundImageImporter : IBackgroundImageImporter
{
    public const long MaxBytes = 50L * 1024 * 1024;
    public const long MaxPixels = 100_000_000;
    public const int MaxEdge = 3840;
    public const string FileName = "background.png";

    private static readonly SKEncodedImageFormat[] _formats = [SKEncodedImageFormat.Png, SKEncodedImageFormat.Jpeg, SKEncodedImageFormat.Webp];

    private readonly IFileSystem _fileSystem;
    private readonly string _looksDirectory;

    public SkiaBackgroundImageImporter(IFileSystem fileSystem, IOptions<DaoReworkOptions> options)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentNullException.ThrowIfNull(options);
        _fileSystem = fileSystem;
        _looksDirectory = options.Value.LooksDirectory;
    }

    public async Task<Result<ImportedPicture>> ImportAsync(string sourcePath, string lookId, CancellationToken cancellationToken = default)
    {
        var folder = LookFolder(lookId);
        if (!_fileSystem.File.Exists(sourcePath))
        {
            return Unreadable(sourcePath, "the file is not there");
        }

        if (_fileSystem.FileInfo.New(sourcePath).Length > MaxBytes)
        {
            return TooLarge(sourcePath, $"it is over {MaxBytes / (1024 * 1024)} MB");
        }

        byte[] bytes;
        try
        {
            bytes = await _fileSystem.File.ReadAllBytesAsync(sourcePath, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Unreadable(sourcePath, exception.Message);
        }

        var decoded = Decode(sourcePath, bytes);
        if (!decoded.TryGetValue(out var original, out var error))
        {
            return Result.Failure<ImportedPicture>(error);
        }

        using (original)
        using (var scaled = ScaleDown(original))
        {
            var brightest = PictureBrightness.Brightest(scaled);
            await SaveAsync(folder, scaled, cancellationToken);
            return Result.Success(new ImportedPicture(FileName, scaled.Width, scaled.Height, brightest));
        }
    }

    public async Task<Result<byte[]>> ReadAsync(string lookId, string fileName, CancellationToken cancellationToken = default)
    {
        var path = _fileSystem.Path.Combine(LookFolder(lookId), _fileSystem.Path.GetFileName(fileName));
        try
        {
            return _fileSystem.File.Exists(path)
                ? Result.Success(await _fileSystem.File.ReadAllBytesAsync(path, cancellationToken))
                : Result.Failure<byte[]>(UnreadableError(path, "the look's copy is missing"));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Result.Failure<byte[]>(UnreadableError(path, exception.Message));
        }
    }

    public Task RemoveAsync(string lookId, string fileName, CancellationToken cancellationToken = default)
    {
        var path = _fileSystem.Path.Combine(LookFolder(lookId), _fileSystem.Path.GetFileName(fileName));
        if (_fileSystem.File.Exists(path))
        {
            _fileSystem.File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private static Result<SKBitmap> Decode(string sourcePath, byte[] bytes)
    {
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data);
        if (codec is null || !_formats.Contains(codec.EncodedFormat))
        {
            return Result.Failure<SKBitmap>(UnreadableError(sourcePath, "it is not a PNG, JPEG or WebP picture"));
        }

        if ((long)codec.Info.Width * codec.Info.Height > MaxPixels)
        {
            return Result.Failure<SKBitmap>(TooLargeError(sourcePath, $"it is {codec.Info.Width} by {codec.Info.Height} pixels"));
        }

        var bitmap = SKBitmap.Decode(codec, codec.Info.WithColorType(SKColorType.Rgba8888).WithAlphaType(SKAlphaType.Unpremul));
        return bitmap is null
            ? Result.Failure<SKBitmap>(UnreadableError(sourcePath, "it could not be decoded"))
            : Result.Success(bitmap);
    }

    private static SKBitmap ScaleDown(SKBitmap original)
    {
        var longEdge = Math.Max(original.Width, original.Height);
        if (longEdge <= MaxEdge)
        {
            return original.Copy();
        }

        var scale = (double)MaxEdge / longEdge;
        var size = new SKImageInfo(Math.Max(1, (int)Math.Round(original.Width * scale)), Math.Max(1, (int)Math.Round(original.Height * scale)), SKColorType.Rgba8888, SKAlphaType.Unpremul);
        return original.Resize(size, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
    }

    private async Task SaveAsync(string folder, SKBitmap picture, CancellationToken cancellationToken)
    {
        _fileSystem.Directory.CreateDirectory(folder);
        using var encoded = picture.Encode(SKEncodedImageFormat.Png, 100);
        var path = _fileSystem.Path.Combine(folder, FileName);
        var temp = $"{path}.{_fileSystem.Path.GetRandomFileName()}.tmp";
        try
        {
            await _fileSystem.File.WriteAllBytesAsync(temp, encoded.ToArray(), cancellationToken);
            _fileSystem.File.Move(temp, path, overwrite: true);
        }
        finally
        {
            if (_fileSystem.File.Exists(temp))
            {
                _fileSystem.File.Delete(temp);
            }
        }
    }

    private string LookFolder(string lookId) =>
        LookNames.IsValidId(lookId)
            ? _fileSystem.Path.Combine(_looksDirectory, lookId)
            : throw new ArgumentException($"'{lookId}' is not a look id.", nameof(lookId)); // The use cases pass a stored look's id.

    private static Result<ImportedPicture> Unreadable(string path, string reason) => Result.Failure<ImportedPicture>(UnreadableError(path, reason));

    private static Result<ImportedPicture> TooLarge(string path, string reason) => Result.Failure<ImportedPicture>(TooLargeError(path, reason));

    private static Error UnreadableError(string path, string reason) =>
        new(DiagnosticCodes.LookImageUnreadable, $"{path} cannot be used as a background: {reason}.", "Pick a PNG, JPEG or WebP picture.");

    private static Error TooLargeError(string path, string reason) =>
        new(DiagnosticCodes.LookImageTooLarge, $"{path} is too large for a background: {reason}.", "Pick a smaller picture, or save a smaller copy first.");
}
