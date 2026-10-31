namespace DAORework.Application.Mods.Backups;

/// <summary>Restores the backup with <paramref name="BackupId"/>, or the newest one for the folder when it is null.</summary>
public sealed record RestoreDeploymentRequest(string DocumentsDirectory, string? BackupId = null);
