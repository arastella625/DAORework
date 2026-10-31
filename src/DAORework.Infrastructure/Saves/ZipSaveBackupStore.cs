using System.Globalization;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Profiles;
using DAORework.Core.Saves;
using DAORework.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace DAORework.Infrastructure.Saves;

/// <summary>
/// Save backups as zips in <c>save-backups/{profile}/{yyyyMMdd-HHmmss-fff}-{4 hex}.zip</c>, each with a
/// <c>manifest.json</c> listing what went in. Sources are opened read-only and shareable (the game may hold one);
/// the zip is built as a <c>.tmp</c> and moved into place. Deletes are refused outside the store's own folder.
/// </summary>
public sealed class ZipSaveBackupStore : ISaveBackupStore
{
    private const string TimestampFormat = "yyyyMMdd-HHmmss-fff";

    private readonly IFileSystem _fileSystem;
    private readonly string _root;
    private readonly TimeProvider _clock;

    public ZipSaveBackupStore(IFileSystem fileSystem, IOptions<DaoReworkOptions> options, TimeProvider clock)
    {
        _fileSystem = fileSystem;
        _root = options.Value.SaveBackupsDirectory;
        _clock = clock;
    }

    public async Task<Result<SaveBackup>> CreateAsync(string profileName, IReadOnlyList<SaveFile> files, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(files);
        var folder = ProfileFolder(profileName);
        _fileSystem.Directory.CreateDirectory(folder);

        var now = _clock.GetUtcNow();
        var path = _fileSystem.Path.Combine(folder, $"{now.ToString(TimestampFormat, CultureInfo.InvariantCulture)}-{RandomNumberGenerator.GetHexString(4, lowercase: true)}.zip");
        var tempPath = path + ".tmp";
        try
        {
            await WriteZipAsync(tempPath, profileName, now, files, cancellationToken);
            _fileSystem.File.Move(tempPath, path, overwrite: false);
            return Result.Success(new SaveBackup(path, now, _fileSystem.FileInfo.New(path).Length));
        }
        catch (IOException exception)
        {
            return Result.Failure<SaveBackup>(new Error(DiagnosticCodes.SaveBackupFailed, $"The backup could not be written: {exception.Message}", "Close the game if it is saving, then try again."));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Result.Failure<SaveBackup>(new Error(DiagnosticCodes.SaveBackupFailed, $"The backup could not be written: {exception.Message}"));
        }
        finally
        {
            if (_fileSystem.File.Exists(tempPath))
            {
                _fileSystem.File.Delete(tempPath);
            }
        }
    }

    public Task<IReadOnlyList<SaveBackup>> ListAsync(string profileName, CancellationToken cancellationToken = default)
    {
        var folder = ProfileFolder(profileName);
        IReadOnlyList<SaveBackup> backups = !_fileSystem.Directory.Exists(folder)
            ? []
            : _fileSystem.Directory.EnumerateFiles(folder, "*.zip")
                .Select(path => (Path: path, Created: ParseCreated(_fileSystem.Path.GetFileNameWithoutExtension(path))))
                .Where(entry => entry.Created is not null)
                .Select(entry => new SaveBackup(entry.Path, entry.Created!.Value, _fileSystem.FileInfo.New(entry.Path).Length))
                .OrderByDescending(b => b.CreatedAtUtc)
                .ThenByDescending(b => b.Path, StringComparer.Ordinal)
                .ToList();

        return Task.FromResult(backups);
    }

    public Task DeleteAsync(SaveBackup backup, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(backup);
        var full = _fileSystem.Path.GetFullPath(backup.Path);
        var root = _fileSystem.Path.GetFullPath(_root) + _fileSystem.Path.DirectorySeparatorChar;
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !full.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Refusing to delete '{backup.Path}': it is not a backup in {_root}.");
        }

        _fileSystem.File.Delete(full);
        return Task.CompletedTask;
    }

    private static DateTimeOffset? ParseCreated(string stem) =>
        stem.Length > TimestampFormat.Length
        && DateTimeOffset.TryParseExact(stem[..TimestampFormat.Length], TimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var created)
            ? created
            : null;

    private string ProfileFolder(string profileName) =>
        ProfileName.Create(profileName).TryGetValue(out var name, out var error)
            ? _fileSystem.Path.Combine(_root, name.Value)
            : throw new ArgumentException(error.Message, nameof(profileName));

    private async Task WriteZipAsync(string tempPath, string profileName, DateTimeOffset now, IReadOnlyList<SaveFile> files, CancellationToken cancellationToken)
    {
        await using var stream = _fileSystem.FileStream.New(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = zip.CreateEntry("Characters/" + file.RelativePath.Replace('\\', '/'), CompressionLevel.Optimal);
            await using var source = _fileSystem.FileStream.New(file.SourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            await using var target = entry.Open();
            await source.CopyToAsync(target, cancellationToken);
        }

        var manifest = new { SchemaVersion = 1, ProfileName = profileName, CreatedAtUtc = now, Files = files.Select(f => new { Path = f.RelativePath, f.SizeBytes }) };
        await using var manifestStream = zip.CreateEntry("manifest.json").Open();
        await JsonSerializer.SerializeAsync(manifestStream, manifest, JsonDefaults.Options, cancellationToken);
    }
}
