using DAORework.Core.Mods.Adoption;

namespace DAORework.Application.Mods.Adoption;

/// <summary>The candidates to adopt, in the order the user confirmed, and the name for the loose override files.</summary>
public sealed record PlanAdoptionRequest(string DocumentsDirectory, IReadOnlyList<AdoptionCandidate> Candidates, string? LooseFilesName = null)
{
    /// <summary>Gets a value indicating whether a candidate that can't be adopted is left out, and named in the plan, instead of failing the whole plan (P2.6-53).</summary>
    public bool SkipFailures { get; init; }
}
