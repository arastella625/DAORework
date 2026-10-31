namespace DAORework.Core.Mods.Adoption;

/// <summary>
/// The library mod an adoption candidate is, or is closest to (ADR-0015). <see cref="DifferingFiles"/> is 0 when every
/// file agrees by place and SHA-256, so adopting imports nothing new; otherwise it counts the files missing, changed
/// or extra, and the candidate would be imported from disk instead.
/// </summary>
public sealed record LibraryMatch(string ModId, string ModName, int DifferingFiles)
{
    public bool IsExact => DifferingFiles == 0;

    /// <summary>Gets where each library file that agrees is in the documents folder, by its library path; all of them when exact.</summary>
    public IReadOnlyDictionary<string, string> Sources { get; init; } = new Dictionary<string, string>();
}
