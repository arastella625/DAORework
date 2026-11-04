using System.IO.Abstractions;
using System.Text.Json;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Downloads;
using DAORework.Infrastructure.Persistence;
using DAORework.Infrastructure.Persistence.Migrations;
using Microsoft.Extensions.Options;

namespace DAORework.Infrastructure.Mods;

/// <summary><c>new-downloads.json</c> under app-data (ADR-0034 §9), replaced atomically.</summary>
public sealed class JsonNewDownloadsStore : INewDownloadsStore
{
    private readonly IFileSystem _fileSystem;
    private readonly string _path;

    public JsonNewDownloadsStore(IFileSystem fileSystem, IOptions<DaoReworkOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _fileSystem = fileSystem;
        _path = options.Value.NewDownloadsFile;
    }

    public async Task<Result<NewDownloadsSettings>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!_fileSystem.File.Exists(_path))
        {
            return Result.Success(NewDownloadsSettings.Off);
        }

        try
        {
            var settings = await MigratingJsonFile.ReadAsync<NewDownloadsSettings>(_fileSystem, _path, MigrationChains.NewDownloads, cancellationToken);
            return Result.Success(settings ?? NewDownloadsSettings.Off);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            return Result.Failure<NewDownloadsSettings>(new Error(DiagnosticCodes.DownloadsUnreadable, $"{_path} could not be read: {exception.Message}"));
        }
    }

    public async Task SaveAsync(NewDownloadsSettings settings, CancellationToken cancellationToken = default)
    {
        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(_path)!);
        await AtomicWrite.TextAsync(_fileSystem, _path, JsonSerializer.Serialize(settings, JsonDefaults.Options), cancellationToken);
    }
}
