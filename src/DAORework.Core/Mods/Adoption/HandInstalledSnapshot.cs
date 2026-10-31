namespace DAORework.Core.Mods.Adoption;

/// <summary>
/// What the adoption scan reads from a documents folder (ADR-0015): the registered add-ins, the files in each
/// <c>AddIns</c> folder and each top-level <c>override</c> folder, the loose files directly in <c>override</c>, and
/// whether Vortex has deployed into <c>override</c> or <c>AddIns</c> (it leaves a <c>vortex.deployment*.json</c> there).
/// Folder names are keys, compared ignoring case.
/// </summary>
public sealed record HandInstalledSnapshot(
    IReadOnlyList<RegisteredAddIn> RegisteredAddIns,
    IReadOnlyDictionary<string, IReadOnlyList<HandInstalledFile>> AddInFolders,
    IReadOnlyDictionary<string, IReadOnlyList<HandInstalledFile>> OverrideFolders,
    IReadOnlyList<HandInstalledFile> LooseOverrideFiles)
{
    public bool VortexDeployedOverride { get; init; }

    public bool VortexDeployedAddIns { get; init; }
}
