using DAORework.Core.Mods.Apply;

namespace DAORework.Application.Abstractions;

/// <summary>
/// The executor's journal for each documents folder (ROADMAP P2-05). The journal is written once, before the first
/// step; each finished step is then appended, so a crash loses at most the step that was running.
/// </summary>
public interface IApplyJournalStore
{
    Task StartAsync(ApplyJournal journal, CancellationToken cancellationToken = default);

    Task MarkDoneAsync(string documentsDirectory, int step, CancellationToken cancellationToken = default);

    /// <summary>The unfinished apply for the folder, or null when there is none.</summary>
    Task<ApplyProgress?> LoadAsync(string documentsDirectory, CancellationToken cancellationToken = default);

    Task DeleteAsync(string documentsDirectory, CancellationToken cancellationToken = default);
}
