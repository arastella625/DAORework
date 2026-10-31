namespace DAORework.Core.Saves;

/// <summary>One backup zip of a profile's saves, as found in DAORework's own save-backups folder.</summary>
public sealed record SaveBackup(string Path, DateTimeOffset CreatedAtUtc, long SizeBytes);
