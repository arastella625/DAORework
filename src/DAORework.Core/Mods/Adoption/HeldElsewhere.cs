namespace DAORework.Core.Mods.Adoption;

/// <summary>What a download looks like (ADR-0039): the mod's name, and what was found in the game when it isn't managed yet.</summary>
public sealed record HeldElsewhere(string Name, AdoptionCandidate? InGame)
{
    /// <summary>Gets a value indicating whether what was found holds nothing else, so managing it takes only this mod.</summary>
    public bool NothingElse { get; init; }
}
