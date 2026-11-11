using DAORework.Application.Abstractions;
using DAORework.Application.Mods.Backups;
using DAORework.Core.Common;
using DAORework.Core.Diagnostics;
using DAORework.Core.Mods.Backups;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Mods.Changes;

/// <summary>
/// Undoes a finished mod change (ROADMAP P2-06): restores its backup (P2-04) and puts back the deployment record from
/// before it, which the backup carries. An unfinished change is settled with recover instead.
/// </summary>
public sealed class RollBackModChange
{
    private readonly IApplyJournalStore _journals;
    private readonly IDeploymentBackupStore _backups;
    private readonly RestoreDeployment _restore;
    private readonly IDeploymentRecordStore _records;
    private readonly IModLibrary _library;

    public RollBackModChange(IApplyJournalStore journals, IDeploymentBackupStore backups, RestoreDeployment restore, IDeploymentRecordStore records, IModLibrary library)
    {
        _library = library;
        _journals = journals;
        _backups = backups;
        _restore = restore;
        _records = records;
    }

    public async Task<Result<RestoreReport>> ExecuteAsync(RollBackModChangeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var documents = request.DocumentsDirectory;
        if (await _journals.LoadAsync(documents, cancellationToken) is not null)
        {
            return Result.Failure<RestoreReport>(new Error(
                DiagnosticCodes.ModApplyUnfinished,
                $"The last change in {documents} did not finish.",
                "Settle it with recover --finish or recover --rollback instead."));
        }

        if (!(await _records.LoadAsync(documents, cancellationToken)).TryGetValue(out var record, out var error))
        {
            return Result.Failure<RestoreReport>(error);
        }

        var backup = Choose(await _backups.ListAsync(documents, cancellationToken), record, request.BackupId);
        if (backup?.PreviousRecord is null)
        {
            return Result.Failure<RestoreReport>(new Error(DiagnosticCodes.ModBackupNotFound, $"There is no change in {documents} to roll back."));
        }

        var library = await _library.ListAsync(cancellationToken);
        if (RemovedMods.Ids(backup.PreviousRecord.ActiveModIds, library.Select(e => e.Mod.Id)) is { Count: > 0 } removed)
        {
            return Result.Failure<RestoreReport>(NeedsRemoved(removed));
        }

        var restored = await _restore.ExecuteAsync(new RestoreDeploymentRequest(documents, backup.Id), cancellationToken);
        if (restored.IsSuccess)
        {
            await _records.SaveAsync(documents, backup.PreviousRecord, cancellationToken);
        }

        return restored;
    }

    /// <summary>
    /// Names the mods as a person reads them, and says what to look for in the Recycle Bin: each mod's folder and
    /// <c>.mod.json</c>, both named by its id (ADR-0039). The window puts "Couldn't undo it:" before the message.
    /// </summary>
    private static Error NeedsRemoved(IReadOnlyList<string> ids)
    {
        var names = string.Join(", ", ids.Select(ModIds.ReadableName));
        var items = string.Join(", ", ids.Select(id => $"{id} and {id}.mod.json"));
        return new Error(
            DiagnosticCodes.ModUndoNeedsRemovedMod,
            ids.Count == 1
                ? $"{names} is no longer in your Library, and this change goes back to it."
                : $"{names} are no longer in your Library, and this change goes back to them.",
            $"Add the same download again, or put back {items} if they're in the Recycle Bin. Then undo this change again. Nothing was changed.");
    }

    /// <summary>
    /// The backup to restore: the one asked for, else the one the record names as its change's. A backup newer than that
    /// belongs to a change that died before its journal was written, so it never happened. A record from before records
    /// named their backup, with something deployed, falls back to the newest; an empty one without one is the start.
    /// </summary>
    private static DeploymentBackup? Choose(IReadOnlyList<DeploymentBackup> backups, DeploymentRecord record, string? requested)
    {
        var wanted = requested ?? record.BackupId;
        if (wanted is not null)
        {
            return backups.FirstOrDefault(b => string.Equals(b.Id, wanted, StringComparison.OrdinalIgnoreCase));
        }

        return record.ActiveModIds.Count == 0 && record.Files.Count == 0 ? null : backups.FirstOrDefault(b => b.PreviousRecord is not null);
    }
}
