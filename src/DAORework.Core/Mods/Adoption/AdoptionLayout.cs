namespace DAORework.Core.Mods.Adoption;

/// <summary>
/// Where an adopted candidate's files go in its library copy (ADR-0015), chosen so <see cref="Library.ModLayout"/> maps
/// each one back to where it is (an add-in's <c>AddIns\&lt;uid&gt;\</c>) or into DAORework's own override folder
/// keeping its layout below the candidate's folder. The importer and the planner both use it, so they agree.
/// </summary>
public static class AdoptionLayout
{
    /// <summary>The manifest an adopted add-in's library copy gets, holding its <c>AddInItem</c> from <c>AddIns.xml</c> as it is.</summary>
    public const string ManifestPath = "Manifest.xml";

    public static string LibraryPath(AdoptionCandidate candidate, HandInstalledFile file)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(file);
        var relative = file.Path[(candidate.Path.Length + 1)..];
        return candidate.Kind == AdoptionCandidateKind.AddIn
            ? $"Contents/addins/{candidate.Path[(candidate.Path.LastIndexOf('/') + 1)..]}/{relative}"
            : relative;
    }

    /// <summary>Every file of the candidate by its library path, with where it is now.</summary>
    public static IReadOnlyDictionary<string, string> Sources(AdoptionCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return candidate.Files.ToDictionary(f => LibraryPath(candidate, f), f => f.Path, StringComparer.OrdinalIgnoreCase);
    }
}
