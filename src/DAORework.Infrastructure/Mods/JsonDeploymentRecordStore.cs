using System.IO.Abstractions;
using System.Text.Json;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Backups;
using DAORework.Core.Mods.Planning;
using DAORework.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace DAORework.Infrastructure.Mods;

/// <summary>Each documents folder's deployment record as <c>deployments/{documentsKey}.json</c>, replaced atomically.</summary>
public sealed class JsonDeploymentRecordStore : IDeploymentRecordStore
{
    private readonly IFileSystem _fileSystem;
    private readonly string _root;

    public JsonDeploymentRecordStore(IFileSystem fileSystem, IOptions<DaoReworkOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _fileSystem = fileSystem;
        _root = options.Value.DeploymentsDirectory;
    }

    public async Task<Result<DeploymentRecord>> LoadAsync(string documentsDirectory, CancellationToken cancellationToken = default)
    {
        var path = PathFor(documentsDirectory);
        if (!_fileSystem.File.Exists(path))
        {
            return Result.Success(DeploymentRecord.Empty);
        }

        try
        {
            await using var stream = _fileSystem.File.OpenRead(path);
            var record = await JsonSerializer.DeserializeAsync<DeploymentRecord>(stream, JsonDefaults.Options, cancellationToken);
            return record is null ? Unreadable(path, "it is empty") : Result.Success(record);
        }
        catch (JsonException exception)
        {
            return Unreadable(path, exception.Message);
        }
    }

    public async Task SaveAsync(string documentsDirectory, DeploymentRecord record, CancellationToken cancellationToken = default)
    {
        _fileSystem.Directory.CreateDirectory(_root);
        await AtomicWrite.TextAsync(_fileSystem, PathFor(documentsDirectory), JsonSerializer.Serialize(record, JsonDefaults.Options), cancellationToken);
    }

    private static Result<DeploymentRecord> Unreadable(string path, string reason) =>
        Result.Failure<DeploymentRecord>(new Error(
            DiagnosticCodes.ModRecordUnreadable,
            $"The deployment record {path} could not be read: {reason}",
            "DAORework will not change mods in this folder until the record is readable again."));

    private string PathFor(string documentsDirectory) => _fileSystem.Path.Combine(_root, DocumentsKey.For(documentsDirectory) + ".json");
}
