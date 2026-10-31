using DAORework.Core.Saves;

namespace DAORework.Application.Saves;

/// <param name="ProfileName">The profile whose documents folder holds the saves.</param>
/// <param name="KeepLatest">How many backups to keep for this profile, the new one included.</param>
public sealed record BackupSavesRequest(string ProfileName, int KeepLatest = SaveBackupRetention.DefaultKeepLatest);
