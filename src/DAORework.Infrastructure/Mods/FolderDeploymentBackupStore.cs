using System.Globalization;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text.Json;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Backups;
using DAORework.Core.Mods.Planning;
using DAORework.Infrastructure.Persistence;
using DAORework.Infrastructure.Persistence.Migrations;
using Microsoft.Extensions.Options;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// Backups of mod changes as plain folders, <c>deployment-backups/{documentsKey}/{yyyyMMdd-HHmmss-fff}-{4 hex}/</c>,
/// holding <c>files/00000</c>... and a <c>manifest.json</c>. A backup is built under a <c>.tmp</c> name and renamed
/// only once every copy is verified and the manifest written, so a half-made backup is never listed or restored.
/// Folders rather than zips, since texture packs are large and restore works file by file.
/// </summary>
public sealed class FolderDeploymentBackupStore : IDeploymentBackupStore
{
    private const string TimestampFormat = "yyyyMMdd-HHmmss-fff";
    private const string ManifestName = "manifest.json";
    private const string StagingSuffix = ".tmp";

    private readonly IFileSystem _fileSystem;
    private readonly string _root;
    private readonly TimeProvider _clock;
    private readonly IModLibrary _library;
    private readonly string _libraryRoot;

    public FolderDeploymentBackupStore(IFileSystem fileSystem, IOptions<DaoReworkOptions> options, TimeProvider clock, IModLibrary library)
    {
        ArgumentNullException.ThrowIfNull(options);
        _fileSystem = fileSystem;
        _root = options.Value.DeploymentBackupsDirectory;
        _clock = clock;
        _library = library;
        _libraryRoot = options.Value.LibraryDirectory;
    }

    public async Task<Result<DeploymentBackup>> SaveAsync(
        string documentsDirectory,
        IReadOnlyList<BackupTarget> targets,
        DeploymentRecord? previousRecord = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(targets);
        var now = _clock.GetUtcNow();
        var id = $"{now.ToString(TimestampFormat, CultureInfo.InvariantCulture)}-{RandomNumberGenerator.GetHexString(4, lowercase: true)}";
        var staging = _fileSystem.Path.Combine(Folder(documentsDirectory), id + StagingSuffix);
        try
        {
            _fileSystem.Directory.CreateDirectory(_fileSystem.Path.Combine(staging, "files"));
            var entries = new List<BackupEntry>();
            for (var i = 0; i < targets.Count; i++)
            {
                var entry = await CaptureAsync(documentsDirectory, targets[i], staging, $"files/{i:D5}", cancellationToken);
                if (!entry.TryGetValue(out var captured, out var error))
                {
                    return Result.Failure<DeploymentBackup>(error);
                }

                entries.Add(captured);
            }

            var backup = new DeploymentBackup(id, now, documentsDirectory, entries) { PreviousRecord = previousRecord };

            // The staging folder's rename below is what makes the backup appear at once, so the manifest is written plainly.
            await _fileSystem.File.WriteAllTextAsync(_fileSystem.Path.Combine(staging, ManifestName), JsonSerializer.Serialize(backup, JsonDefaults.Options), cancellationToken);
            _fileSystem.Directory.Move(staging, staging[..^StagingSuffix.Length]);
            return Result.Success(backup);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Failed($"The backup could not be written: {exception.Message}");
        }
        finally
        {
            if (_fileSystem.Directory.Exists(staging))
            {
                _fileSystem.Directory.Delete(staging, recursive: true);
            }
        }
    }

    public async Task<IReadOnlyList<DeploymentBackup>> ListAsync(string documentsDirectory, CancellationToken cancellationToken = default)
    {
        var folder = Folder(documentsDirectory);
        if (!_fileSystem.Directory.Exists(folder))
        {
            return [];
        }

        var backups = new List<DeploymentBackup>();
        foreach (var manifest in _fileSystem.Directory.EnumerateDirectories(folder)
            .Where(d => !d.EndsWith(StagingSuffix, StringComparison.Ordinal))
            .Select(d => _fileSystem.Path.Combine(d, ManifestName))
            .Where(_fileSystem.File.Exists))
        {
            var backup = await MigratingJsonFile.ReadAsync<DeploymentBackup>(_fileSystem, manifest, MigrationChains.DeploymentBackups, cancellationToken);
            if (backup is not null)
            {
                backups.Add(backup);
            }
        }

        return backups.OrderByDescending(b => b.CreatedAtUtc).ThenByDescending(b => b.Id, StringComparer.Ordinal).ToList();
    }

    public async Task<Result<RestoreReport>> RestoreAsync(DeploymentBackup backup, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backup);
        var folder = _fileSystem.Path.Combine(Folder(backup.DocumentsDirectory), backup.Id);
        if (!_fileSystem.Directory.Exists(folder))
        {
            return Result.Failure<RestoreReport>(new Error(DiagnosticCodes.ModBackupNotFound, $"The backup {backup.Id} is no longer in {_root}."));
        }

        var restorer = new BackupRestorer(_fileSystem, backup, folder, new MovedFileRestorer(_fileSystem, backup.DocumentsDirectory, FindInLibraryAsync));
        try
        {
            return await restorer.RestoreAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Result.Failure<RestoreReport>(new Error(
                DiagnosticCodes.ModRestoreFailed,
                $"Restoring backup {backup.Id} stopped: {exception.Message}",
                "Close the game and anything using the documents folder, then roll back again; the backup is kept."));
        }
    }

    private static Result<DeploymentBackup> Failed(string message) =>
        Result.Failure<DeploymentBackup>(new Error(DiagnosticCodes.ModBackupFailed, message, "Nothing was changed. Close the game and try again."));

    private static string? PresenceProblem(BackupTarget target, bool exists) =>
        (exists, target.AnyContent, target.ExpectedSha256) switch
        {
            (true, false, null) => $"{target.TargetPath} appeared after the plan was made.",
            (false, false, not null) => $"{target.TargetPath} disappeared after the plan was made.",
            _ => null,
        };

    private async Task<Result<BackupEntry>> CaptureAsync(string documents, BackupTarget target, string staging, string storedAs, CancellationToken cancellationToken)
    {
        if (target.MovedFrom is { } movedFrom)
        {
            return await CaptureMoveAsync(documents, target, movedFrom, cancellationToken);
        }

        var path = DocumentsPaths.Resolve(_fileSystem, documents, target.TargetPath);
        var exists = _fileSystem.File.Exists(path);
        if (PresenceProblem(target, exists) is { } problem)
        {
            return Result.Failure<BackupEntry>(new Error(DiagnosticCodes.ModBackupFailed, problem, "Make the plan again."));
        }

        if (!exists)
        {
            return Result.Success(new BackupEntry(target.TargetPath, null, null, target.AfterSha256));
        }

        var copy = _fileSystem.Path.Combine(staging, storedAs);
        var sourceHash = await FileHashes.CopyAsync(_fileSystem, path, copy, cancellationToken);
        if (target.ExpectedSha256 is not null && !FileHashes.Same(sourceHash, target.ExpectedSha256))
        {
            return Result.Failure<BackupEntry>(new Error(DiagnosticCodes.ModBackupFailed, $"{target.TargetPath} changed after the plan was made.", "Make the plan again."));
        }

        if (!FileHashes.Same(await FileHashes.ComputeAsync(_fileSystem, copy, cancellationToken), sourceHash))
        {
            return Result.Failure<BackupEntry>(new Error(DiagnosticCodes.ModBackupFailed, $"The backup copy of {target.TargetPath} does not match the original."));
        }

        return Result.Success(new BackupEntry(target.TargetPath, sourceHash, storedAs, target.AfterSha256));
    }

    /// <summary>A move is only recorded: nothing may be at the target, and the file must still be where it is moved from.</summary>
    private async Task<Result<BackupEntry>> CaptureMoveAsync(string documents, BackupTarget target, string movedFrom, CancellationToken cancellationToken)
    {
        var from = DocumentsPaths.Resolve(_fileSystem, documents, movedFrom);
        if (_fileSystem.File.Exists(DocumentsPaths.Resolve(_fileSystem, documents, target.TargetPath)))
        {
            return Result.Failure<BackupEntry>(new Error(DiagnosticCodes.ModBackupFailed, $"{target.TargetPath} appeared after the plan was made.", "Make the plan again."));
        }

        if (target.AfterSha256 is null || !_fileSystem.File.Exists(from) || !FileHashes.Same(await FileHashes.ComputeAsync(_fileSystem, from, cancellationToken), target.AfterSha256))
        {
            return Result.Failure<BackupEntry>(new Error(DiagnosticCodes.ModBackupFailed, $"{movedFrom} changed or disappeared after the plan was made.", "Make the plan again."));
        }

        return Result.Success(new BackupEntry(target.TargetPath, null, null, target.AfterSha256) { MovedFrom = movedFrom });
    }

    /// <summary>A file in the library with this SHA-256, for a moved file that is gone (ADR-0015).</summary>
    private async Task<string?> FindInLibraryAsync(string sha256, CancellationToken cancellationToken)
    {
        foreach (var entry in await _library.ListAsync(cancellationToken))
        {
            foreach (var file in entry.Mod.Files.Where(f => FileHashes.Same(f.Sha256, sha256)))
            {
                var path = _fileSystem.Path.Combine(_libraryRoot, entry.Mod.Id, file.RelativePath.Replace('/', _fileSystem.Path.DirectorySeparatorChar));
                if (_fileSystem.File.Exists(path))
                {
                    return path;
                }
            }
        }

        return null;
    }

    private string Folder(string documentsDirectory) => _fileSystem.Path.Combine(_root, DocumentsKey.For(documentsDirectory));
}
