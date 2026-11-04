using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Conflicts;

/// <summary>
/// More than one copy of an override file has the same name, from two mods or from folders in one. Only
/// <see cref="Winner"/>, the last in the profile's order, is deployed (ADR-0012: one file name, one deployed copy).
/// </summary>
public sealed record OverrideConflict(string FileName, DeployedFile Winner, IReadOnlyList<DeployedFile> Overridden)
{
    /// <summary>Gets a value indicating whether every copy has the same content, in which case the conflict changes nothing in the game.</summary>
    public bool SameContent => Overridden.All(o => string.Equals(o.Source.Sha256, Winner.Source.Sha256, StringComparison.OrdinalIgnoreCase));

    /// <summary>Gets each other mod that has a copy, once. Empty when the copies are all in the winner's own mod.</summary>
    public IReadOnlyList<string> OtherModIds =>
        [.. Overridden.Select(o => o.ModId).Distinct(StringComparer.OrdinalIgnoreCase).Where(id => !string.Equals(id, Winner.ModId, StringComparison.OrdinalIgnoreCase))];
}
