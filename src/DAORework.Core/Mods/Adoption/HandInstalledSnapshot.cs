namespace DAORework.Core.Mods.Adoption;

/// <summary>
/// What the adoption scan reads from a documents folder (ADR-0015): the registered add-ins, the files in each
/// <c>AddIns</c> folder and each top-level <c>override</c> folder, the loose files directly in <c>override</c>, and
/// Vortex's records in <c>override</c> (ADR-0028), and whether Vortex left a record in <c>override</c> that could not be
/// read or any record in <c>AddIns</c>. Folder names are keys, compared ignoring case.
/// </summary>
public sealed record HandInstalledSnapshot(
    IReadOnlyList<RegisteredAddIn> RegisteredAddIns,
    IReadOnlyDictionary<string, IReadOnlyList<HandInstalledFile>> AddInFolders,
    IReadOnlyDictionary<string, IReadOnlyList<HandInstalledFile>> OverrideFolders,
    IReadOnlyList<HandInstalledFile> LooseOverrideFiles)
{
    public bool VortexDeployedOverride { get; init; }

    public bool VortexDeployedAddIns { get; init; }

    /// <summary>Gets Vortex's records in <c>override</c> that could be read, whose mods become candidates of their own.</summary>
    public IReadOnlyList<VortexRecord> VortexRecords { get; init; } = [];
}
