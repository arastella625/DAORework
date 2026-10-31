namespace DAORework.Core.Mods.Apply;

/// <summary>A journal found on disk and the steps it records as done: an apply that did not finish.</summary>
public sealed record ApplyProgress(ApplyJournal Journal, IReadOnlySet<int> DoneSteps);
