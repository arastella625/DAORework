using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods;
using Microsoft.Extensions.Logging;

namespace DAORework.Application.Mods;

/// <summary>Lists every file in the mod folders. A file that cannot be read is listed with <see cref="ModFile.ReadError"/> set, never dropped.</summary>
public sealed class BuildModInventory
{
    private readonly IFileSystem _fileSystem;
    private readonly IFileHasher _fileHasher;
    private readonly IKnownFolders _knownFolders;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BuildModInventory> _logger;

    public BuildModInventory(
        IFileSystem fileSystem,
        IFileHasher fileHasher,
        IKnownFolders knownFolders,
        TimeProvider timeProvider,
        ILogger<BuildModInventory> logger)
    {
        _fileSystem = fileSystem;
        _fileHasher = fileHasher;
        _knownFolders = knownFolders;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result<ModInventory>> ExecuteAsync(BuildModInventoryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var documents = DocumentsDirectoryResolver.Resolve(request.Profile, _knownFolders);
        var items = new List<ModFile>();

        foreach (var (source, root) in new[] { ("Override", GamePaths.OverrideDirectory(documents)), (GamePaths.AddInsDirectoryName, GamePaths.AddInsDirectory(documents)) })
        {
            var folder = await InventoryFolderAsync(source, root, request.IncludeHashes, cancellationToken);
            if (!folder.TryGetValue(out var files, out var error))
            {
                return Result.Failure<ModInventory>(error);
            }

            items.AddRange(files);
        }

        var inventory = new ModInventory
        {
            CreatedAtUtc = _timeProvider.GetUtcNow(),
            ProfileName = request.Profile.Name,
            UserDocumentsDirectory = documents,
            IncludesHashes = request.IncludeHashes,
            Items = items.OrderBy(x => x.SourceFolder).ThenBy(x => x.RelativePath, StringComparer.OrdinalIgnoreCase).ToList(),
        };

        _logger.LogInformation(
            "Created DAO mod inventory for profile {ProfileName}. Files: {FileCount}. Unreadable: {UnreadableCount}. Hashes: {IncludesHashes}",
            inventory.ProfileName,
            inventory.TotalFiles,
            inventory.UnreadableFileCount,
            inventory.IncludesHashes);

        return Result.Success(inventory);
    }

    private async Task<Result<List<ModFile>>> InventoryFolderAsync(string sourceFolder, string rootPath, bool includeHashes, CancellationToken cancellationToken)
    {
        var items = new List<ModFile>();

        if (!_fileSystem.Directory.Exists(rootPath))
        {
            return Result.Success(items);
        }

        try
        {
            foreach (var filePath in _fileSystem.Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                items.Add(await ReadFileAsync(sourceFolder, rootPath, filePath, includeHashes, cancellationToken));
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Result.Failure<List<ModFile>>(new Error(
                DiagnosticCodes.ModFolderUnreadable,
                $"The {sourceFolder} folder could not be listed: {rootPath}. {exception.Message}",
                "Check the folder's permissions, then run the command again."));
        }

        return Result.Success(items);
    }

    private async Task<ModFile> ReadFileAsync(string sourceFolder, string rootPath, string filePath, bool includeHashes, CancellationToken cancellationToken)
    {
        var fileInfo = _fileSystem.FileInfo.New(filePath);
        var item = new ModFile
        {
            SourceFolder = sourceFolder,
            RootPath = rootPath,
            FullPath = filePath,
            RelativePath = Path.GetRelativePath(rootPath, filePath),
            FileName = fileInfo.Name,
            Extension = fileInfo.Extension,
        };

        try
        {
            item = item with { SizeBytes = fileInfo.Length, LastModifiedUtc = fileInfo.LastWriteTimeUtc };
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return item with { ReadError = exception.Message };
        }

        if (!includeHashes)
        {
            return item;
        }

        var hash = await _fileHasher.ComputeSha256Async(filePath, cancellationToken);
        return hash.TryGetValue(out var value, out var error)
            ? item with { Sha256Hash = value }
            : item with { ReadError = error.Message };
    }
}
