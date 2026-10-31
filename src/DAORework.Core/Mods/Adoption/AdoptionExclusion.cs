namespace DAORework.Core.Mods.Adoption;

/// <summary>Something hand-installed that the scan found but cannot be adopted, and why.</summary>
public sealed record AdoptionExclusion(string Id, string Name, AdoptionExclusionReason Reason);
