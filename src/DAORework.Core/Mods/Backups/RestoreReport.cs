namespace DAORework.Core.Mods.Backups;

/// <summary>
/// What a restore did: files put back, files the plan had created and that were removed again, and created paths
/// left alone because their content was no longer what the plan wrote (someone else's file now).
/// </summary>
public sealed record RestoreReport(int Restored, int Removed, IReadOnlyList<string> LeftInPlace);
