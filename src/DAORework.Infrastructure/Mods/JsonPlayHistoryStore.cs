using System.IO.Abstractions;
using System.Text.Json;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Backups;
using DAORework.Core.Mods.Troubleshooting;
using DAORework.Infrastructure.Persistence;
using DAORework.Infrastructure.Persistence.Migrations;
using Microsoft.Extensions.Options;

namespace DAORework.Infrastructure.Mods;

/// <summary>Each documents folder's recent games (ADR-0043), as <c>play-history/{documentsKey}.json</c>, replaced atomically.</summary>
public sealed class JsonPlayHistoryStore : IPlayHistoryStore
{
    private readonly IFileSystem _fileSystem;
    private readonly string _root;

    public JsonPlayHistoryStore(IFileSystem fileSystem, IOptions<DaoReworkOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _fileSystem = fileSystem;
        _root = options.Value.PlayHistoryDirectory;
    }

    public async Task<Result<PlayHistory>> LoadAsync(string documentsDirectory, CancellationToken cancellationToken = default)
    {
        var path = PathFor(documentsDirectory);
        if (!_fileSystem.File.Exists(path))
        {
            return Result.Success(PlayHistory.Empty);
        }

        try
        {
            var history = await MigratingJsonFile.ReadAsync<PlayHistory>(_fileSystem, path, MigrationChains.PlayHistories, cancellationToken);
            return history is null ? Unreadable(path, "it is empty") : Result.Success(history);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            return Unreadable(path, exception.Message);
        }
    }

    public async Task SaveAsync(string documentsDirectory, PlayHistory history, CancellationToken cancellationToken = default)
    {
        _fileSystem.Directory.CreateDirectory(_root);
        await AtomicWrite.TextAsync(_fileSystem, PathFor(documentsDirectory), JsonSerializer.Serialize(history, JsonDefaults.Options), cancellationToken);
    }

    private static Result<PlayHistory> Unreadable(string path, string reason) =>
        Result.Failure<PlayHistory>(new Error(DiagnosticCodes.PlayHistoryUnreadable, $"The list of recent games in {path} could not be read: {reason}"));

    private string PathFor(string documentsDirectory) => _fileSystem.Path.Combine(_root, DocumentsKey.For(documentsDirectory) + ".json");
}
