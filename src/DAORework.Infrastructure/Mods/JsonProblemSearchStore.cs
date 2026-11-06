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

/// <summary>Each documents folder's search for the problem mod (ADR-0042), as <c>problem-searches/{documentsKey}.json</c>, replaced atomically.</summary>
public sealed class JsonProblemSearchStore : IProblemSearchStore
{
    private readonly IFileSystem _fileSystem;
    private readonly string _root;

    public JsonProblemSearchStore(IFileSystem fileSystem, IOptions<DaoReworkOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _fileSystem = fileSystem;
        _root = options.Value.ProblemSearchesDirectory;
    }

    public async Task<Result<ProblemSearch?>> LoadAsync(string documentsDirectory, CancellationToken cancellationToken = default)
    {
        var path = PathFor(documentsDirectory);
        if (!_fileSystem.File.Exists(path))
        {
            return Result.Success<ProblemSearch?>(null);
        }

        try
        {
            var search = await MigratingJsonFile.ReadAsync<ProblemSearch>(_fileSystem, path, MigrationChains.ProblemSearches, cancellationToken);
            return search is null ? Unreadable(path, "it is empty") : Result.Success<ProblemSearch?>(search);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            return Unreadable(path, exception.Message);
        }
    }

    public async Task SaveAsync(string documentsDirectory, ProblemSearch search, CancellationToken cancellationToken = default)
    {
        _fileSystem.Directory.CreateDirectory(_root);
        await AtomicWrite.TextAsync(_fileSystem, PathFor(documentsDirectory), JsonSerializer.Serialize(search, JsonDefaults.Options), cancellationToken);
    }

    public void Delete(string documentsDirectory)
    {
        var path = PathFor(documentsDirectory);
        if (_fileSystem.File.Exists(path))
        {
            _fileSystem.File.Delete(path);
        }
    }

    private static Result<ProblemSearch?> Unreadable(string path, string reason) =>
        Result.Failure<ProblemSearch?>(new Error(DiagnosticCodes.ModSearchUnreadable, $"The search for the problem mod in {path} could not be read: {reason}", "Start a new search to replace it."));

    private string PathFor(string documentsDirectory) => _fileSystem.Path.Combine(_root, DocumentsKey.For(documentsDirectory) + ".json");
}
