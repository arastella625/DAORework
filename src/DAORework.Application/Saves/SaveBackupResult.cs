using DAORework.Core.Saves;

namespace DAORework.Application.Saves;

/// <summary>The backup just made, what went into it, and which older backups retention removed.</summary>
public sealed record SaveBackupResult(SaveBackup Backup, int FileCount, long SourceBytes, IReadOnlyList<SaveBackup> Deleted);
