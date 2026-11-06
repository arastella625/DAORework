using DAORework.Core.Common;
using DAORework.Core.Mods.Troubleshooting;

namespace DAORework.Application.Abstractions;

/// <summary>Keeps a documents folder's search for the problem mod between rounds, which can be days apart (ADR-0042).</summary>
public interface IProblemSearchStore
{
    /// <summary>The search under way, or null when there is none.</summary>
    Task<Result<ProblemSearch?>> LoadAsync(string documentsDirectory, CancellationToken cancellationToken = default);

    Task SaveAsync(string documentsDirectory, ProblemSearch search, CancellationToken cancellationToken = default);

    void Delete(string documentsDirectory);
}
