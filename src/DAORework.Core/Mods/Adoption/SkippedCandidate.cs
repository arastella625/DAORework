namespace DAORework.Core.Mods.Adoption;

/// <summary>Something found in the game that a batch went on without, and why (P2.6-53).</summary>
/// <param name="CandidateId">Which candidate it was.</param>
/// <param name="Name">What the player knows it as.</param>
/// <param name="Why">What went wrong.</param>
public sealed record SkippedCandidate(string CandidateId, string Name, string Why);
