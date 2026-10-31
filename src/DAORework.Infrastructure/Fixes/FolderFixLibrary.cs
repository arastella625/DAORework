using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text.Json;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Fixes;
using DAORework.Infrastructure.Mods;
using DAORework.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace DAORework.Infrastructure.Fixes;

/// <summary>
/// The fixes in the library (ADR-0013): each entry's files in <c>library/fixes/{id}/</c>, written to a <c>.tmp</c>
/// folder first and renamed into place, then its <c>fix.json</c> last, so an entry is listed only once all of it is there.
/// </summary>
public sealed class FolderFixLibrary : IFixLibrary
{
    public const string MetadataFileName = "fix.json";

    private readonly IFileSystem _fileSystem;
    private readonly string _root;

    public FolderFixLibrary(IFileSystem fileSystem, IOptions<DaoReworkOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _fileSystem = fileSystem;
        _root = fileSystem.Path.Combine(options.Value.LibraryDirectory, FixIds.LibraryFolder);
    }

    public async Task<IReadOnlyList<FixEntry>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!_fileSystem.Directory.Exists(_root))
        {
            return [];
        }

        var entries = new List<FixEntry>();
        foreach (var folder in _fileSystem.Directory.EnumerateDirectories(_root))
        {
            if (await ReadAsync(_fileSystem.Path.Combine(folder, MetadataFileName), cancellationToken) is { } entry)
            {
                entries.Add(entry);
            }
        }

        return entries.OrderBy(e => e.Id, StringComparer.Ordinal).ToList();
    }

    public async Task<Result<FixEntry>> AddAsync(FixEntry entry, IReadOnlyDictionary<string, byte[]> contents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(contents);
        var folder = _fileSystem.Path.Combine(_root, entry.Id);
        var metadata = _fileSystem.Path.Combine(folder, MetadataFileName);
        if (await ReadAsync(metadata, cancellationToken) is { } existing)
        {
            return Result.Success(existing);
        }

        var files = entry.Files.Select(f => f.Applied).Concat(entry.Kept).ToList();
        var mismatch = files.FirstOrDefault(f => !contents.TryGetValue(f.RelativePath, out var bytes) || !FileHashes.Same(Convert.ToHexStringLower(SHA256.HashData(bytes)), f.Sha256));
        if (mismatch is not null)
        {
            return Failed($"The content for {mismatch.RelativePath} of {entry.Id} does not match its hash; nothing was stored.");
        }

        try
        {
            await WriteAsync(folder, files.Select(f => (f.RelativePath, contents[f.RelativePath])), cancellationToken);
            await AtomicWrite.TextAsync(_fileSystem, metadata, JsonSerializer.Serialize(entry, JsonDefaults.Options), cancellationToken);
            return Result.Success(entry);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Failed($"{entry.Id} could not be stored in the library: {exception.Message}");
        }
    }

    private static Result<FixEntry> Failed(string message) => Result.Failure<FixEntry>(new Error(DiagnosticCodes.FixLibraryFailed, message));

    private async Task WriteAsync(string folder, IEnumerable<(string RelativePath, byte[] Content)> files, CancellationToken cancellationToken)
    {
        var staging = folder + ".tmp";
        foreach (var leftover in new[] { staging, folder }.Where(_fileSystem.Directory.Exists))
        {
            _fileSystem.Directory.Delete(leftover, recursive: true);
        }

        foreach (var (relative, content) in files)
        {
            var path = DocumentsPaths.Resolve(_fileSystem, staging, relative);
            _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(path)!);
            await _fileSystem.File.WriteAllBytesAsync(path, content, cancellationToken);
        }

        _fileSystem.Directory.Move(staging, folder);
    }

    private async Task<FixEntry?> ReadAsync(string metadata, CancellationToken cancellationToken)
    {
        if (!_fileSystem.File.Exists(metadata))
        {
            return null;
        }

        // A fix.json that does not parse belongs to an entry that never finished being stored; adding it again replaces it.
        try
        {
            await using var stream = _fileSystem.File.OpenRead(metadata);
            return await JsonSerializer.DeserializeAsync<FixEntry>(stream, JsonDefaults.Options, cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
