using System.Globalization;
using System.IO.Abstractions;
using System.Text.Json;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Core.Mods.Apply;
using DAORework.Core.Mods.Backups;
using DAORework.Infrastructure.Persistence;
using DAORework.Infrastructure.Persistence.Migrations;
using Microsoft.Extensions.Options;

namespace DAORework.Infrastructure.Mods;

/// <summary>
/// The apply journal as <c>deployments/{documentsKey}.journal.json</c> (written once, atomically) and
/// <c>.journal.log</c>, one finished step number per line, appended as each step finishes. A line cut short by a
/// crash is not a number and is ignored: that step simply runs again, which is safe because every step is.
/// </summary>
public sealed class FileApplyJournalStore : IApplyJournalStore
{
    private readonly IFileSystem _fileSystem;
    private readonly string _root;

    public FileApplyJournalStore(IFileSystem fileSystem, IOptions<DaoReworkOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _fileSystem = fileSystem;
        _root = options.Value.DeploymentsDirectory;
    }

    public async Task StartAsync(ApplyJournal journal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(journal);
        _fileSystem.Directory.CreateDirectory(_root);
        var (json, log) = Paths(journal.DocumentsDirectory);
        await _fileSystem.File.WriteAllTextAsync(log, string.Empty, cancellationToken);
        await AtomicWrite.TextAsync(_fileSystem, json, JsonSerializer.Serialize(journal, JsonDefaults.Options), cancellationToken);
    }

    public Task MarkDoneAsync(string documentsDirectory, int step, CancellationToken cancellationToken = default) =>
        _fileSystem.File.AppendAllTextAsync(Paths(documentsDirectory).Log, step.ToString(CultureInfo.InvariantCulture) + "\n", cancellationToken);

    public async Task<ApplyProgress?> LoadAsync(string documentsDirectory, CancellationToken cancellationToken = default)
    {
        var (json, log) = Paths(documentsDirectory);
        if (!_fileSystem.File.Exists(json))
        {
            return null;
        }

        var journal = await MigratingJsonFile.ReadAsync<ApplyJournal>(_fileSystem, json, MigrationChains.ApplyJournals, cancellationToken);
        if (journal is null)
        {
            return null;
        }

        var done = _fileSystem.File.Exists(log)
            ? (await _fileSystem.File.ReadAllLinesAsync(log, cancellationToken))
                .Select(line => int.TryParse(line, NumberStyles.None, CultureInfo.InvariantCulture, out var step) ? step : -1)
                .Where(step => step >= 0 && step < journal.StepCount)
                .ToHashSet()
            : [];
        return new ApplyProgress(journal, done);
    }

    public Task DeleteAsync(string documentsDirectory, CancellationToken cancellationToken = default)
    {
        // The journal file goes first: without it there is no unfinished apply, whatever the log still says.
        var (json, log) = Paths(documentsDirectory);
        _fileSystem.File.Delete(json);
        _fileSystem.File.Delete(log);
        return Task.CompletedTask;
    }

    private (string Json, string Log) Paths(string documentsDirectory)
    {
        var stem = _fileSystem.Path.Combine(_root, DocumentsKey.For(documentsDirectory) + ".journal");
        return (stem + ".json", stem + ".log");
    }
}
