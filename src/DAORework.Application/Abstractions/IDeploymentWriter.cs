using DAORework.Core.Common;
using DAORework.Core.Mods.Planning;

namespace DAORework.Application.Abstractions;

/// <summary>
/// The only code that writes mod files into the documents folder (ADR-0012, ROADMAP P2-05). Every call is safe to
/// repeat: a step whose target already holds its end state succeeds without writing, and a target in any state other
/// than the plan's before- or after-state is refused.
/// </summary>
public interface IDeploymentWriter
{
    /// <summary>Copies from the library, replaces or deletes one file, checking its hash before and after.</summary>
    Task<Result> ApplyAsync(string documentsDirectory, PlanOperation operation, CancellationToken cancellationToken = default);

    /// <summary>Adds each AddInItem from its mod's manifest in the library, or removes it; nothing else in the file changes.</summary>
    Task<Result> EditAddInsListAsync(string documentsDirectory, IReadOnlyList<AddInsListChange> changes, CancellationToken cancellationToken = default);
}
