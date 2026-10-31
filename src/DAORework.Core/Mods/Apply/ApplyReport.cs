namespace DAORework.Core.Mods.Apply;

/// <summary>What an apply (or a finished recovery) did, and the backup that can undo it.</summary>
public sealed record ApplyReport(string BackupId, int StepsRun, int StepsAlreadyDone);
