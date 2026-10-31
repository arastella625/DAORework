using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Conflicts;

/// <summary>
/// Two or more active mods ship an override file with the same name. Only <see cref="Winner"/>, from the mod latest in
/// the profile's order, is deployed (ADR-0012: one file name, one deployed copy).
/// </summary>
public sealed record OverrideConflict(string FileName, DeployedFile Winner, IReadOnlyList<DeployedFile> Overridden)
{
    /// <summary>Gets a value indicating whether every copy has the same content, in which case the conflict changes nothing in the game.</summary>
    public bool SameContent => Overridden.All(o => string.Equals(o.Source.Sha256, Winner.Source.Sha256, StringComparison.OrdinalIgnoreCase));
}
