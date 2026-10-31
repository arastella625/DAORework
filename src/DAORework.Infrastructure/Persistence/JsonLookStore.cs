using System.IO.Abstractions;
using System.Text.Json;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Looks;
using DAORework.Infrastructure.Persistence.Migrations;
using Microsoft.Extensions.Options;

namespace DAORework.Infrastructure.Persistence;

/// <summary>
/// One folder per custom look under <c>looks\</c>: its <c>look.json</c> and its picture (ADR-0017). A look id is also
/// the folder's name, so an id that is not in <see cref="LookNames"/>' strict form is never turned into a path.
/// </summary>
public sealed class JsonLookStore : ILookStore
{
    private const string FileName = "look.json";

    private readonly IFileSystem _fileSystem;
    private readonly string _looksDirectory;

    public JsonLookStore(IFileSystem fileSystem, IOptions<DaoReworkOptions> options)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentNullException.ThrowIfNull(options);
        _fileSystem = fileSystem;
        _looksDirectory = options.Value.LooksDirectory;
    }

    public async Task<LookListing> ListAsync(CancellationToken cancellationToken = default)
    {
        var looks = new List<Look>();
        var unreadable = new List<Error>();
        if (!_fileSystem.Directory.Exists(_looksDirectory))
        {
            return new LookListing(looks, unreadable);
        }

        foreach (var id in _fileSystem.Directory.EnumerateDirectories(_looksDirectory).Select(_fileSystem.Path.GetFileName).Where(LookNames.IsValidId))
        {
            var loaded = await LoadAsync(id!, cancellationToken); // IsValidId is false for null.
            if (loaded.TryGetValue(out var look, out var error))
            {
                looks.Add(look);
            }
            else if (error.Code == DiagnosticCodes.LookUnreadable)
            {
                unreadable.Add(error);
            }
        }

        return new LookListing(looks, unreadable);
    }

    public async Task<Result<Look>> LoadAsync(string lookId, CancellationToken cancellationToken = default)
    {
        var path = LookFile(lookId);
        if (path is null || !_fileSystem.File.Exists(path))
        {
            return Result.Failure<Look>(new Error(DiagnosticCodes.LookNotFound, $"There is no look '{lookId}'."));
        }

        try
        {
            var look = await MigratingJsonFile.ReadAsync<Look>(_fileSystem, path, MigrationChains.Looks, cancellationToken);
            return look is not null && look.Id == lookId
                ? Result.Success(look)
                : Unreadable(path, "its id does not match its folder");
        }
        catch (Exception exception) when (exception is InvalidDataException or JsonException or IOException or UnauthorizedAccessException)
        {
            return Unreadable(path, exception.Message);
        }
    }

    public Task SaveAsync(Look look, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(look);
        var path = LookFile(look.Id) ?? throw new ArgumentException($"'{look.Id}' is not a look id.", nameof(look)); // The use cases make ids.
        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(path)!);
        return AtomicJsonFile.WriteAsync(_fileSystem, path, look with { SchemaVersion = Look.CurrentSchemaVersion }, cancellationToken);
    }

    public Task DeleteAsync(string lookId, CancellationToken cancellationToken = default)
    {
        var path = LookFile(lookId);
        var folder = path is null ? null : _fileSystem.Path.GetDirectoryName(path);
        if (folder is not null && _fileSystem.Directory.Exists(folder))
        {
            _fileSystem.Directory.Delete(folder, recursive: true);
        }

        return Task.CompletedTask;
    }

    private string? LookFile(string? lookId) =>
        LookNames.IsValidId(lookId) ? _fileSystem.Path.Combine(_looksDirectory, lookId!, FileName) : null; // Checked: not null.

    private static Result<Look> Unreadable(string path, string reason) =>
        Result.Failure<Look>(new Error(DiagnosticCodes.LookUnreadable, $"The look in {path} could not be read: {reason}", "Delete the look and make it again."));
}
