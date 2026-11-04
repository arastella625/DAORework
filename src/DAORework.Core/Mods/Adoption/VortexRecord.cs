namespace DAORework.Core.Mods.Adoption;

/// <summary>
/// What one of Vortex's deployment records (<c>vortex.deployment*.json</c>) says, as ADR-0028 reads it: the files
/// Vortex placed and the Vortex mod each came from. Whether Vortex can still reach them is worked out by the reader:
/// <see cref="AtItsTarget"/> when the record sits in the folder its <c>targetPath</c> names, and <see cref="StagedMods"/>
/// for the mods its staging folder still holds.
/// </summary>
public sealed record VortexRecord(IReadOnlyList<VortexPlacedFile> Files)
{
    public bool AtItsTarget { get; init; }

    public IReadOnlySet<string> StagedMods { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the staging folder the record names, wherever it is; each mod is a folder in it (ADR-0028 §3).</summary>
    public string StagingPath { get; init; } = string.Empty;

    /// <summary>Gets the folder below the documents folder that the record's paths start with, with its slash.</summary>
    public string RelativeFolder { get; init; } = string.Empty;

    /// <summary>Whether Vortex can still put this mod's files back, so moving them would not last (ADR-0028 §2).</summary>
    public bool IsLive(string mod) => AtItsTarget && StagedMods.Contains(mod);
}
