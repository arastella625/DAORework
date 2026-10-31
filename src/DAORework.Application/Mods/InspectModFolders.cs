using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods;

namespace DAORework.Application.Mods;

/// <summary>Summarizes the override and AddIns folders: counts, readable size, and how many files could not be read.</summary>
public sealed class InspectModFolders
{
    private readonly IFileSystem _fileSystem;
    private readonly IKnownFolders _knownFolders;
    private readonly TimeProvider _timeProvider;

    public InspectModFolders(IFileSystem fileSystem, IKnownFolders knownFolders, TimeProvider timeProvider)
    {
        _fileSystem = fileSystem;
        _knownFolders = knownFolders;
        _timeProvider = timeProvider;
    }

    public Task<Result<ModInspectionReport>> ExecuteAsync(InspectModFoldersRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();
        var documents = DocumentsDirectoryResolver.Resolve(request.Profile, _knownFolders);

        if (!Summarize("Override Folder", GamePaths.OverrideDirectory(documents)).TryGetValue(out var overrideFolder, out var error) ||
            !Summarize("AddIns Folder", GamePaths.AddInsDirectory(documents)).TryGetValue(out var addInsFolder, out error))
        {
            return Task.FromResult(Result.Failure<ModInspectionReport>(error));
        }

        return Task.FromResult(Result.Success(new ModInspectionReport
        {
            CreatedAtUtc = _timeProvider.GetUtcNow(),
            ProfileName = request.Profile.Name,
            UserDocumentsDirectory = documents,
            OverrideFolder = overrideFolder,
            AddInsFolder = addInsFolder,
        }));
    }

    private Result<ModFolderSummary> Summarize(string name, string path)
    {
        if (!_fileSystem.Directory.Exists(path))
        {
            return Result.Success(new ModFolderSummary { Name = name, Path = path, Exists = false });
        }

        try
        {
            var files = _fileSystem.Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).ToList();
            var folderCount = _fileSystem.Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories).Count();

            long sizeBytes = 0;
            var unreadable = 0;

            foreach (var file in files)
            {
                if (TryReadLength(file, out var length))
                {
                    sizeBytes += length;
                }
                else
                {
                    unreadable++;
                }
            }

            return Result.Success(new ModFolderSummary
            {
                Name = name,
                Path = path,
                Exists = true,
                FileCount = files.Count,
                FolderCount = folderCount,
                SizeBytes = sizeBytes,
                UnreadableFileCount = unreadable,
            });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Result.Failure<ModFolderSummary>(new Error(
                DiagnosticCodes.ModFolderUnreadable,
                $"The {name.ToLowerInvariant()} could not be listed: {path}. {exception.Message}",
                "Check the folder's permissions, then run the command again."));
        }
    }

    private bool TryReadLength(string file, out long length)
    {
        try
        {
            length = _fileSystem.FileInfo.New(file).Length;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            length = 0;
            return false;
        }
    }
}
