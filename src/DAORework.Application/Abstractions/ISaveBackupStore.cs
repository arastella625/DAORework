using DAORework.Core.Common;
using DAORework.Core.Saves;

namespace DAORework.Application.Abstractions;

/// <summary>
/// DAORework's own store of save backups, one folder per profile under the app-data folder. It only ever reads the
/// source saves and only ever writes or deletes inside its own folder (the Phase 1 exit gate).
/// </summary>
public interface ISaveBackupStore
{
    /// <summary>Copies <paramref name="files"/> into a new zip. A failure leaves nothing behind.</summary>
    Task<Result<SaveBackup>> CreateAsync(string profileName, IReadOnlyList<SaveFile> files, CancellationToken cancellationToken = default);

    /// <summary>The profile's backups, newest first.</summary>
    Task<IReadOnlyList<SaveBackup>> ListAsync(string profileName, CancellationToken cancellationToken = default);

    Task DeleteAsync(SaveBackup backup, CancellationToken cancellationToken = default);
}
