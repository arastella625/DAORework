using System.Formats.Tar;
using System.IO.Abstractions;
using System.IO.Compression;
using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Infrastructure.Fixes;

/// <summary>
/// Reads the one DLL a fix needs out of a release the user supplies (ADR-0013): a <c>.tar.gz</c> as DXVK publishes
/// it, a <c>.zip</c>, an extracted folder, or the DLL itself. Archives are read in memory; nothing is extracted to disk.
/// </summary>
public sealed class FixSourceReader : IFixSourceReader
{
    /// <summary>The largest DLL accepted; DXVK's d3d9.dll is a few MB.</summary>
    public const long MaxDllBytes = 64L * 1024 * 1024;

    private const string DxvkDllPath = "x32/d3d9.dll";
    private const string DllName = "d3d9.dll";

    private readonly IFileSystem _fileSystem;

    public FixSourceReader(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<Result<byte[]>> ReadDxvkDllAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        try
        {
            if (_fileSystem.Directory.Exists(sourcePath))
            {
                return await FromFolderAsync(sourcePath, cancellationToken);
            }

            if (!_fileSystem.File.Exists(sourcePath))
            {
                return Invalid($"{sourcePath} does not exist.");
            }

            var name = _fileSystem.Path.GetFileName(sourcePath).ToLowerInvariant();
            return name switch
            {
                _ when name.EndsWith(".dll", StringComparison.Ordinal) => await ReadDllAsync(sourcePath, cancellationToken),
                _ when name.EndsWith(".zip", StringComparison.Ordinal) => await FromZipAsync(sourcePath, cancellationToken),
                _ when name.EndsWith(".tar.gz", StringComparison.Ordinal) || name.EndsWith(".tgz", StringComparison.Ordinal) => await FromTarGzAsync(sourcePath, cancellationToken),
                _ => Invalid($"{sourcePath} is not a .tar.gz, a .zip, a folder or a DLL.", "Extract a .7z first, or give the path of the release's x32\\d3d9.dll."),
            };
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or FormatException)
        {
            return Invalid($"{sourcePath} could not be read: {exception.Message}");
        }
    }

    private static bool IsDxvkDll(string entryName) =>
        entryName.Replace('\\', '/').EndsWith(DxvkDllPath, StringComparison.OrdinalIgnoreCase);

    private static Result<byte[]> Invalid(string message, string? recommendation = null) =>
        Result.Failure<byte[]>(new Error(DiagnosticCodes.FixSourceInvalid, message, recommendation));

    private static Result<byte[]> NotInRelease(string source) =>
        Invalid($"{source} has no {DxvkDllPath}.", "Give a DXVK release from https://github.com/doitsujin/dxvk/releases, or its x32\\d3d9.dll.");

    private static async Task<Result<byte[]>> ReadAsync(Stream stream, long length, CancellationToken cancellationToken)
    {
        if (length > MaxDllBytes)
        {
            return Invalid($"The DLL is larger than {MaxDllBytes / (1024 * 1024)} MB, too large to be DXVK's.");
        }

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return Result.Success(buffer.ToArray());
    }

    private async Task<Result<byte[]>> ReadDllAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = _fileSystem.File.OpenRead(path);
        return await ReadAsync(stream, stream.Length, cancellationToken);
    }

    private async Task<Result<byte[]>> FromFolderAsync(string folder, CancellationToken cancellationToken)
    {
        var dll = _fileSystem.Directory.EnumerateFiles(folder, DllName, SearchOption.AllDirectories)
            .Where(p => IsDxvkDll(_fileSystem.Path.GetRelativePath(folder, p)))
            .OrderBy(p => p.Length)
            .FirstOrDefault();
        return dll is null ? NotInRelease(folder) : await ReadDllAsync(dll, cancellationToken);
    }

    private async Task<Result<byte[]>> FromZipAsync(string path, CancellationToken cancellationToken)
    {
        await using var file = _fileSystem.File.OpenRead(path);
        using var zip = new ZipArchive(file, ZipArchiveMode.Read);
        var entry = zip.Entries.Where(e => IsDxvkDll(e.FullName)).OrderBy(e => e.FullName.Length).FirstOrDefault();
        if (entry is null)
        {
            return NotInRelease(path);
        }

        await using var stream = entry.Open();
        return await ReadAsync(stream, entry.Length, cancellationToken);
    }

    private async Task<Result<byte[]>> FromTarGzAsync(string path, CancellationToken cancellationToken)
    {
        await using var file = _fileSystem.File.OpenRead(path);
        await using var gzip = new GZipStream(file, CompressionMode.Decompress);
        await using var tar = new TarReader(gzip);
        while (await tar.GetNextEntryAsync(copyData: false, cancellationToken) is { } entry)
        {
            if (entry.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile && IsDxvkDll(entry.Name) && entry.DataStream is { } data)
            {
                return await ReadAsync(data, entry.Length, cancellationToken);
            }
        }

        return NotInRelease(path);
    }
}
