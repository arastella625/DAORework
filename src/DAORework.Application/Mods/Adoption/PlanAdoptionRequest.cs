using DAORework.Core.Mods.Adoption;

namespace DAORework.Application.Mods.Adoption;

/// <summary>The candidates to adopt, in the order the user confirmed, and the name for the loose override files.</summary>
public sealed record PlanAdoptionRequest(string DocumentsDirectory, IReadOnlyList<AdoptionCandidate> Candidates, string? LooseFilesName = null);
