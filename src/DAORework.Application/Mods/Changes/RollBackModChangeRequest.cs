namespace DAORework.Application.Mods.Changes;

/// <summary>Undoes the change whose backup is <paramref name="BackupId"/>, or the most recent change when it is null.</summary>
public sealed record RollBackModChangeRequest(string DocumentsDirectory, string? BackupId = null);
