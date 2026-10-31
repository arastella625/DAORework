using System.IO.Abstractions;
using DAORework.Application.Abstractions;
using DAORework.Application.Mods;
using DAORework.Application.Profiles;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Saves;

namespace DAORework.Application.Saves;

/// <summary>
/// Copies every file under the profile's <c>Characters</c> folder into a new backup, then applies retention.
/// The saves folder is only read; retention only deletes DAORework's own older backups, and only after the new one exists.
/// </summary>
public sealed class BackupSaves
{
    private readonly GetProfile _getProfile;
    private readonly ISaveBackupStore _store;
    private readonly IFileSystem _fileSystem;
    private readonly IKnownFolders _knownFolders;

    public BackupSaves(GetProfile getProfile, ISaveBackupStore store, IFileSystem fileSystem, IKnownFolders knownFolders)
    {
        _getProfile = getProfile;
        _store = store;
        _fileSystem = fileSystem;
        _knownFolders = knownFolders;
    }

    public async Task<Result<SaveBackupResult>> ExecuteAsync(BackupSavesRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SaveBackupRetention.Create(request.KeepLatest).TryGetValue(out var retention, out var retentionError))
        {
            return Result.Failure<SaveBackupResult>(retentionError);
        }

        if (!(await _getProfile.ExecuteAsync(new GetProfileRequest(request.ProfileName), cancellationToken)).TryGetValue(out var profile, out var profileError))
        {
            return Result.Failure<SaveBackupResult>(profileError);
        }

        var characters = GamePaths.CharactersDirectory(DocumentsDirectoryResolver.Resolve(profile, _knownFolders));
        var files = SaveFiles(characters);
        if (files.Count == 0)
        {
            return Result.Failure<SaveBackupResult>(new Error(
                DiagnosticCodes.SaveBackupNothingToBackUp,
                $"There are no saves to back up in {characters}.",
                "Saves appear there once you have played and saved; check the profile's documents folder if you have."));
        }

        if (!(await _store.CreateAsync(profile.Name, files, cancellationToken)).TryGetValue(out var backup, out var backupError))
        {
            return Result.Failure<SaveBackupResult>(backupError);
        }

        var expired = retention.Expired(await _store.ListAsync(profile.Name, cancellationToken));
        foreach (var old in expired)
        {
            await _store.DeleteAsync(old, cancellationToken);
        }

        return Result.Success(new SaveBackupResult(backup, files.Count, files.Sum(f => f.SizeBytes), expired));
    }

    private List<SaveFile> SaveFiles(string characters)
    {
        if (!_fileSystem.Directory.Exists(characters))
        {
            return [];
        }

        return _fileSystem.Directory.EnumerateFiles(characters, "*", SearchOption.AllDirectories)
            .Select(path => new SaveFile(path, Path.GetRelativePath(characters, path), _fileSystem.FileInfo.New(path).Length))
            .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
