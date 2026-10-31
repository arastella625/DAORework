using DAORework.Core.Common;
using DAORework.Core.Diagnostics;

namespace DAORework.Core.Saves;

/// <summary>How many backups to keep per profile. Older ones are deleted after a successful backup, never before.</summary>
public sealed record SaveBackupRetention
{
    public const int DefaultKeepLatest = 10;
    public const int MaximumKeepLatest = 1000;

    private SaveBackupRetention(int keepLatest)
    {
        KeepLatest = keepLatest;
    }

    public static SaveBackupRetention Default { get; } = new(DefaultKeepLatest);

    public int KeepLatest { get; }

    public static Result<SaveBackupRetention> Create(int keepLatest) =>
        keepLatest is >= 1 and <= MaximumKeepLatest
            ? Result.Success(new SaveBackupRetention(keepLatest))
            : Result.Failure<SaveBackupRetention>(new Error(
                DiagnosticCodes.SaveBackupRetentionInvalid,
                $"Keep between 1 and {MaximumKeepLatest} backups (got {keepLatest})."));

    /// <summary>The backups to delete: everything but the newest <see cref="KeepLatest"/>.</summary>
    public IReadOnlyList<SaveBackup> Expired(IEnumerable<SaveBackup> backups)
    {
        ArgumentNullException.ThrowIfNull(backups);
        return backups.OrderByDescending(b => b.CreatedAtUtc).ThenByDescending(b => b.Path, StringComparer.Ordinal).Skip(KeepLatest).ToList();
    }
}
