namespace DAORework.Core.Mods.Adoption;

/// <summary>The result of the adoption scan (ADR-0015, ROADMAP P2.5-02): what can be adopted, and what cannot and why.</summary>
public sealed record AdoptionScan(IReadOnlyList<AdoptionCandidate> Candidates, IReadOnlyList<AdoptionExclusion> Exclusions);
