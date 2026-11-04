using System.IO.Abstractions;
using System.Text.Json;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Backups;
using DAORework.Core.Mods.Handover;
using DAORework.Infrastructure.Persistence;
using DAORework.Infrastructure.Persistence.Migrations;
using Microsoft.Extensions.Options;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// Each documents folder's handover from Vortex (ADR-0028 §3) as <c>vortex-handovers/{documentsKey}.json</c>, replaced
/// atomically, with <c>Settings\AddIns.xml</c> kept beside it as <c>{documentsKey}.AddIns.xml</c>.
/// </summary>
public sealed class JsonVortexHandoverStore : IVortexHandoverStore
{
    private readonly IFileSystem _fileSystem;
    private readonly string _root;

    public JsonVortexHandoverStore(IFileSystem fileSystem, IOptions<DaoReworkOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _fileSystem = fileSystem;
        _root = options.Value.VortexHandoversDirectory;
    }

    public async Task<Result<VortexHandover?>> LoadAsync(string documentsDirectory, CancellationToken cancellationToken = default)
    {
        var path = PathFor(documentsDirectory, ".json");
        if (!_fileSystem.File.Exists(path))
        {
            return Result.Success<VortexHandover?>(null);
        }

        try
        {
            var handover = await MigratingJsonFile.ReadAsync<VortexHandover>(_fileSystem, path, MigrationChains.VortexHandovers, cancellationToken);
            return handover is null ? Unreadable(path, "it is empty") : Result.Success<VortexHandover?>(handover);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            return Unreadable(path, exception.Message);
        }
    }

    public async Task SaveAsync(string documentsDirectory, VortexHandover handover, CancellationToken cancellationToken = default)
    {
        _fileSystem.Directory.CreateDirectory(_root);
        await AtomicWrite.TextAsync(_fileSystem, PathFor(documentsDirectory, ".json"), JsonSerializer.Serialize(handover, JsonDefaults.Options), cancellationToken);
    }

    public async Task KeepAddInsListAsync(string documentsDirectory, CancellationToken cancellationToken = default)
    {
        var list = DocumentsPaths.Resolve(_fileSystem, documentsDirectory, "Settings/AddIns.xml");
        var kept = PathFor(documentsDirectory, ".AddIns.xml");
        if (_fileSystem.File.Exists(list) && !_fileSystem.File.Exists(kept))
        {
            _fileSystem.Directory.CreateDirectory(_root);
            await AtomicWrite.TextAsync(_fileSystem, kept, await _fileSystem.File.ReadAllTextAsync(list, cancellationToken), cancellationToken);
        }
    }

    private static Result<VortexHandover?> Unreadable(string path, string reason) =>
        Result.Failure<VortexHandover?>(new Error(DiagnosticCodes.VortexHandoverUnreadable, $"The handover from Vortex in {path} could not be read: {reason}"));

    private string PathFor(string documentsDirectory, string suffix) => _fileSystem.Path.Combine(_root, DocumentsKey.For(documentsDirectory) + suffix);
}
