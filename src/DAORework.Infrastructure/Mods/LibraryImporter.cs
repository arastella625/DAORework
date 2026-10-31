using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text.Json;
using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Safety;
using DAORework.Infrastructure.Persistence;
using DAORework.Infrastructure.Persistence.Migrations;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// The library's import pipeline (ADR-0012, ROADMAP P2-07), shared by everything that adds a mod to the library: the
/// source's shape is classified, a wrapped <c>.dazip</c> is unwrapped, the mod is named, its files are extracted into a
/// <c>.tmp</c> folder, scanned by the user's antivirus (ADR-0018) and only then renamed into place, and its
/// <c>mod.json</c> is written last. A detection deletes the <c>.tmp</c> folder, so nothing of the mod is kept. A
/// source already in the library is found by its id and not imported again.
/// </summary>
internal sealed class LibraryImporter
{
    public const string MetadataSuffix = ".mod.json";

    private readonly IFileSystem _fileSystem;
    private readonly string _root;
    private readonly TimeProvider _clock;
    private readonly LibraryFileClassifier _classifier;
    private readonly IMalwareScanner? _scanner;

    /// <summary>Imports go through <paramref name="scanner"/>, the antivirus scan; without one, they are recorded as not scanned.</summary>
    public LibraryImporter(IFileSystem fileSystem, string root, TimeProvider clock, IMalwareScanner? scanner = null)
    {
        _scanner = scanner;
        _fileSystem = fileSystem;
        _root = root;
        _clock = clock;
        _classifier = new LibraryFileClassifier(fileSystem, root);
    }

    /// <summary>Imports one opened source, or finds it already in the library.</summary>
    public Task<Result<LibraryImport>> ImportAsync(ModSource source, CancellationToken cancellationToken) =>
        ImportAsync(source, ModOrigin.Download, cancellationToken);

    /// <summary>Imports one opened source with where it came from (ADR-0015); a source already in the library keeps its entry.</summary>
    public async Task<Result<LibraryImport>> ImportAsync(ModSource source, ModOrigin origin, CancellationToken cancellationToken)
    {
        if (!ArchiveClassifier.Classify(source.Files).TryGetValue(out var shape, out var error))
        {
            return Result.Failure<LibraryImport>(error);
        }

        if (shape.Shape == ArchiveShape.WrappedAddIn)
        {
            using var inner = await UnwrapAsync(source, shape.InnerDazip!, cancellationToken);
            return await ImportAsync(inner, origin, cancellationToken);
        }

        var described = await DescribeAsync(source, shape.Shape);
        if (!described.TryGetValue(out var mod, out error))
        {
            return Result.Failure<LibraryImport>(error);
        }

        var (name, uid) = mod;
        var id = ModIds.Create(name, source.Sha256);
        if (await ReadAsync(MetadataPath(id), cancellationToken) is { } existing)
        {
            return Result.Success(new LibraryImport(existing, AlreadyInLibrary: true));
        }

        var kind = uid is null ? ModKind.Override : ModKind.AddIn;
        var (files, staging) = await ExtractAsync(source, id, cancellationToken);
        var scan = await ScanAsync(files, staging, cancellationToken);
        if (scan.Status == MalwareScanStatus.ThreatFound)
        {
            _fileSystem.Directory.Delete(staging, recursive: true);
            return Result.Failure<LibraryImport>(new Error(
                DiagnosticCodes.ModSafetyThreatFound,
                $"{source.Name} was not imported: {scan.Detail}",
                "Nothing of it was kept. If you are sure this is a false positive, deal with it in your antivirus, then import again."));
        }

        _fileSystem.Directory.Move(staging, _fileSystem.Path.Combine(_root, id));
        var entry = new LibraryEntry(new LibraryMod(id, name, kind, files, uid), source.Name, source.Sha256, _clock.GetUtcNow()) { Origin = origin, Scan = scan };
        await AtomicWrite.TextAsync(_fileSystem, MetadataPath(id), JsonSerializer.Serialize(entry, JsonDefaults.Options), cancellationToken);
        return Result.Success(new LibraryImport(entry, AlreadyInLibrary: false));
    }

    /// <summary>A mod.json, read through its migration chain; null when it is missing or an import never finished.</summary>
    public async Task<LibraryEntry?> ReadAsync(string metadata, CancellationToken cancellationToken)
    {
        if (!_fileSystem.File.Exists(metadata))
        {
            return null;
        }

        // A metadata file that does not parse is an import that never finished; importing again replaces it. One from
        // a newer build is refused instead (InvalidDataException), so it is never replaced.
        try
        {
            var entry = await MigratingJsonFile.ReadAsync<LibraryEntry>(_fileSystem, metadata, MigrationChains.LibraryEntries, cancellationToken);
            return entry is null ? null : await _classifier.ClassifyUnclassifiedAsync(entry, metadata, cancellationToken);
        }
        catch (InvalidDataException exception) when (exception.InnerException is JsonException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static async Task<ZipModSource> UnwrapAsync(ModSource outer, string dazip, CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();
        await using (var stream = outer.Open(dazip))
        {
            await stream.CopyToAsync(buffer, cancellationToken);
        }

        buffer.Position = 0;
        var sha = Convert.ToHexStringLower(await SHA256.HashDataAsync(buffer, cancellationToken));
        buffer.Position = 0;
        return new ZipModSource(dazip[(dazip.LastIndexOf('/') + 1)..], sha, buffer);
    }

    private static async Task<string> ReadTextAsync(ModSource source, string file)
    {
        var actual = source.Files.First(f => f.Equals(file, StringComparison.OrdinalIgnoreCase));
        using var reader = new StreamReader(source.Open(actual));
        return await reader.ReadToEndAsync();
    }

    /// <summary>Checks the mod's size and finds its name, and its UID when it is an add-in.</summary>
    private async Task<Result<(string Name, string? Uid)>> DescribeAsync(ModSource source, ArchiveShape shape)
    {
        if (source.Files.Count > FolderModLibrary.MaxFiles || source.TotalBytes > FolderModLibrary.MaxBytes)
        {
            return Result.Failure<(string Name, string? Uid)>(new Error(
                DiagnosticCodes.ModImportInvalid,
                $"{source.Name} holds {source.Files.Count} files, {new ByteSize(source.TotalBytes)} in all: more than any mod DAORework accepts."));
        }

        if (shape != ArchiveShape.AddIn)
        {
            return Result.Success<(string Name, string? Uid)>((ModIds.NameFromFileName(_fileSystem.Path.GetFileNameWithoutExtension(source.Name)), null));
        }

        return AddInManifest.Parse(await ReadTextAsync(source, "Manifest.xml")).TryGetValue(out var manifest, out var error)
            ? Result.Success<(string Name, string? Uid)>((manifest.Name, manifest.Uid))
            : Result.Failure<(string Name, string? Uid)>(error);
    }

    /// <summary>The extracted files and the <c>.tmp</c> folder holding them, not yet renamed into place.</summary>
    private async Task<(List<LibraryModFile> Files, string Staging)> ExtractAsync(ModSource source, string id, CancellationToken cancellationToken)
    {
        var folder = _fileSystem.Path.Combine(_root, id);
        var staging = folder + ".tmp";
        foreach (var leftover in new[] { staging, folder }.Where(_fileSystem.Directory.Exists))
        {
            _fileSystem.Directory.Delete(leftover, recursive: true);
        }

        var files = new List<LibraryModFile>();
        await source.CopyEachAsync(
            async (file, input) =>
            {
                var target = _fileSystem.Path.Combine(staging, file.Replace('/', _fileSystem.Path.DirectorySeparatorChar));
                _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(target)!);
                var (sha, size) = await FileHashes.WriteAsync(_fileSystem, input, target, cancellationToken);
                files.Add(new LibraryModFile(file, size, sha) { Kind = await _classifier.ClassifyAsync(target, file, cancellationToken) });
            },
            cancellationToken);
        return (files.OrderBy(f => f.RelativePath, StringComparer.Ordinal).ToList(), staging);
    }

    private Task<MalwareScan> ScanAsync(List<LibraryModFile> files, string staging, CancellationToken cancellationToken) =>
        _scanner is null
            ? Task.FromResult(new MalwareScan(MalwareScanStatus.NotScanned, "No antivirus scan was set up."))
            : _scanner.ScanAsync(
                files.Select(f => new ScanTarget(f.RelativePath, _fileSystem.Path.Combine(staging, f.RelativePath.Replace('/', _fileSystem.Path.DirectorySeparatorChar)), f.Kind == ModFileKind.Program)).ToList(),
                cancellationToken);

    private string MetadataPath(string id) => _fileSystem.Path.Combine(_root, id + MetadataSuffix);
}
