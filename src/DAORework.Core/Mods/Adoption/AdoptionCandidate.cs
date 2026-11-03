using DAORework.Core.Catalog;
using DAORework.Core.Mods.Safety;

namespace DAORework.Core.Mods.Adoption;

/// <summary>
/// A hand-installed mod DAORework can adopt (ADR-0015). <see cref="Id"/> is stable and is what the CLI and the GUI
/// pass back; <see cref="Path"/> is its folder relative to the documents folder, with <c>/</c>.
/// </summary>
public sealed record AdoptionCandidate(string Id, AdoptionCandidateKind Kind, string Name, string Path, IReadOnlyList<HandInstalledFile> Files)
{
    /// <summary>Gets the known-items catalog entry one of its fingerprints matches, if any.</summary>
    public KnownItem? CatalogItem { get; init; }

    /// <summary>
    /// Gets a value indicating whether Vortex can still put its files back, so a move would not last: a live Vortex mod
    /// (ADR-0028 §2), or a folder where Vortex left a record that could not be read.
    /// </summary>
    public bool DeployedByVortex { get; init; }

    /// <summary>Gets the library mod it is, or is closest to, when the library holds one with files in common.</summary>
    public LibraryMatch? LibraryMatch { get; init; }

    public long SizeBytes => Files.Sum(f => f.SizeBytes);

    /// <summary>Gets its program files (ADR-0018), which an adoption never moves and DAORework never installs.</summary>
    public IReadOnlyList<HandInstalledFile> Programs => Files.Where(f => ProgramFiles.IsProgram(f.Path)).ToList();
}
