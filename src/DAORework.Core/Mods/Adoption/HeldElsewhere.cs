namespace DAORework.Core.Mods.Adoption;

/// <summary>What a download looks like (ADR-0039): the mod's name, and what was found in the game when it isn't managed yet.</summary>
public sealed record HeldElsewhere(string Name, AdoptionCandidate? InGame);
