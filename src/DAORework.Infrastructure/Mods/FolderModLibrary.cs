using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using Microsoft.Extensions.Options;
using SharpCompress.Common;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// The mod library (ADR-0012, ROADMAP P2-07): each mod's files, exactly as its download lays them out, in
/// <c>library/{modId}/</c>, and its <c>mod.json</c> beside that folder as <c>library/{modId}.mod.json</c>. The files
/// of a <c>.dazip</c>, <c>.zip</c>, <c>.7z</c>, <c>.rar</c> (P2.5-04) or folder are extracted into a <c>.tmp</c>
/// folder, renamed into place, and the JSON is written last, so a mod is in the library only once all of it is. The documents folder and the download itself are only ever read.
/// </summary>
public sealed class FolderModLibrary : IModLibrary
{
    /// <summary>More files than any real Dragon Age mod has; an archive past it is treated as malformed.</summary>
    public const int MaxFiles = 100_000;

    /// <summary>The largest mod accepted once extracted: 16 GB, well past the biggest texture packs.</summary>
    public const long MaxBytes = 16L * 1024 * 1024 * 1024;

    private readonly IFileSystem _fileSystem;
    private readonly string _root;
    private readonly LibraryImporter _importer;

    /// <summary>Every import goes through <paramref name="scanner"/>, the antivirus scan (ADR-0018); without one, imports are recorded as not scanned.</summary>
    public FolderModLibrary(IFileSystem fileSystem, IOptions<DaoReworkOptions> options, TimeProvider clock, IMalwareScanner? scanner = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        _fileSystem = fileSystem;
        _root = options.Value.LibraryDirectory;
        _importer = new LibraryImporter(fileSystem, _root, clock, scanner);
    }

    public async Task<Result<LibraryImport>> ImportAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var opened = await OpenAsync(sourcePath, cancellationToken);
            if (!opened.TryGetValue(out var source, out var error))
            {
                return Result.Failure<LibraryImport>(error);
            }

            using (source)
            {
                return await _importer.ImportAsync(source, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return Result.Failure<LibraryImport>(new Error(DiagnosticCodes.ModImportFailed, $"{sourcePath} could not be imported: {exception.Message}"));
        }
        catch (SharpCompressException exception)
        {
            return Result.Failure<LibraryImport>(new Error(
                DiagnosticCodes.ModImportInvalid,
                $"{sourcePath} could not be read as an archive: {exception.Message}",
                "It may be damaged, password-protected or one part of a split archive. Download it again from the mod's page."));
        }
    }

    public async Task<IReadOnlyList<LibraryEntry>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!_fileSystem.Directory.Exists(_root))
        {
            return [];
        }

        var entries = new List<LibraryEntry>();
        foreach (var metadata in _fileSystem.Directory.EnumerateFiles(_root, "*" + LibraryImporter.MetadataSuffix))
        {
            if (await _importer.ReadAsync(metadata, cancellationToken) is { } entry)
            {
                entries.Add(entry);
            }
        }

        return entries.OrderBy(e => e.Mod.Name, StringComparer.OrdinalIgnoreCase).ThenBy(e => e.Mod.Id, StringComparer.Ordinal).ToList();
    }

    private static Result<ModSource> Unsupported(string path) =>
        Result.Failure<ModSource>(new Error(
            DiagnosticCodes.ModImportUnsupported,
            $"{path} is not a .dazip, .zip, .7z or .rar, or a folder.",
            "Extract it with an archive tool such as 7-Zip, then import the extracted folder."));

    private async Task<Result<ModSource>> OpenAsync(string path, CancellationToken cancellationToken)
    {
        if (_fileSystem.Directory.Exists(path))
        {
            return Result.Success<ModSource>(await FolderModSource.CreateAsync(_fileSystem, path, cancellationToken));
        }

        if (!_fileSystem.File.Exists(path))
        {
            return Result.Failure<ModSource>(new Error(DiagnosticCodes.ModImportFailed, $"{path} does not exist."));
        }

        var extension = _fileSystem.Path.GetExtension(path).ToLowerInvariant();
        if (extension is not (".zip" or ".dazip" or ".7z" or ".rar"))
        {
            return Unsupported(path);
        }

        var sha = await FileHashes.ComputeAsync(_fileSystem, path, cancellationToken);
        var name = _fileSystem.Path.GetFileName(path);
        var stream = _fileSystem.File.OpenRead(path);
        try
        {
            return Result.Success<ModSource>(extension is ".7z" or ".rar" ? new CompressedModSource(name, sha, stream) : new ZipModSource(name, sha, stream));
        }
        catch
        {
            await stream.DisposeAsync();
            throw;
        }
    }
}
