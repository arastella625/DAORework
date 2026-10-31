using DAORework.Core.Mods.Library;

namespace DAORework.Core.Mods.Adoption;

/// <summary>
/// Matches adoption candidates to mods already in the library (ADR-0015, "matched to a library mod"). An add-in is
/// compared with the library add-ins of its UID, file by file where DAUpdater.exe puts them; an override folder or the
/// loose files with each library override mod that shares a file name, file by file by name. Only files whose size
/// agrees are hashed, so <see cref="PathsToHash"/> comes first and <see cref="Match"/> takes the hashes. Pure.
/// </summary>
public static class AdoptionLibraryMatcher
{
    /// <summary>The documents paths whose SHA-256 the match needs: files where a library mod's file would be, with its size.</summary>
    public static IReadOnlyList<string> PathsToHash(HandInstalledSnapshot snapshot, AdoptionScan scan, IReadOnlyList<LibraryMod> library)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(scan);
        ArgumentNullException.ThrowIfNull(library);

        var sizes = Sizes(snapshot);
        return scan.Candidates
            .SelectMany(candidate => Rivals(candidate, library).SelectMany(mod => Pair(candidate, mod, sizes)))
            .Select(pair => pair.DiskPath)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>The scan with each candidate's closest library mod: the one with the fewest differing files among those with any file in common.</summary>
    public static AdoptionScan Match(HandInstalledSnapshot snapshot, AdoptionScan scan, IReadOnlyList<LibraryMod> library, IReadOnlyDictionary<string, string> hashes)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(scan);
        ArgumentNullException.ThrowIfNull(library);
        ArgumentNullException.ThrowIfNull(hashes);

        var sizes = Sizes(snapshot);
        return scan with { Candidates = scan.Candidates.Select(c => c with { LibraryMatch = Closest(c, library, sizes, hashes) }).ToList() };
    }

    private static LibraryMatch? Closest(AdoptionCandidate candidate, IReadOnlyList<LibraryMod> library, Dictionary<string, long> sizes, IReadOnlyDictionary<string, string> hashes)
    {
        LibraryMatch? closest = null;
        foreach (var mod in Rivals(candidate, library))
        {
            var pairs = Pair(candidate, mod, sizes);
            var sources = pairs
                .Where(p => p.DiskPath is { } path && hashes.TryGetValue(path, out var sha) && sha.Equals(p.File.Sha256, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(p => p.File.RelativePath, p => p.DiskPath ?? string.Empty, StringComparer.OrdinalIgnoreCase);
            var agreeing = sources.Values.ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (agreeing.Count == 0)
            {
                continue;
            }

            var differing = pairs.Count - agreeing.Count + candidate.Files.Count(f => !agreeing.Contains(f.Path));
            if (closest is null || differing < closest.DifferingFiles)
            {
                closest = new LibraryMatch(mod.Id, mod.Name, differing) { Sources = sources };
            }
        }

        return closest;
    }

    private static IEnumerable<LibraryMod> Rivals(AdoptionCandidate candidate, IReadOnlyList<LibraryMod> library)
    {
        if (candidate.Kind == AdoptionCandidateKind.AddIn)
        {
            var uid = FileName(candidate.Path);
            return library.Where(m => m.Kind == ModKind.AddIn && uid.Equals(m.AddInUid, StringComparison.OrdinalIgnoreCase));
        }

        var names = candidate.Files.Select(f => FileName(f.Path)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return library.Where(m => m.Kind == ModKind.Override && m.Files.Any(f => names.Contains(FileName(f.RelativePath))));
    }

    /// <summary>Each of the library mod's deployed files with where it is on disk with the same size, or null when it is not.</summary>
    private static List<(LibraryModFile File, string? DiskPath)> Pair(AdoptionCandidate candidate, LibraryMod mod, Dictionary<string, long> sizes)
    {
        if (mod.Kind == ModKind.AddIn)
        {
            var owned = $"{ModLayout.OverridePrefix}{ModLayout.OwnedOverrideFolder}/{mod.Id}/";
            return ModLayout.Map(mod).TryGetValue(out var deployed, out _)
                ? deployed.Select(d => (d.Source, Present(DaUpdaterPath(d, owned), d.Source.SizeBytes, sizes))).ToList()
                : [];
        }

        var unused = candidate.Files.ToList();
        var pairs = new List<(LibraryModFile File, string? DiskPath)>();
        foreach (var file in mod.Files)
        {
            var match = unused.FirstOrDefault(f => f.SizeBytes == file.SizeBytes && f.Path.EndsWith("/" + file.RelativePath, StringComparison.OrdinalIgnoreCase))
                ?? unused.FirstOrDefault(f => f.SizeBytes == file.SizeBytes && FileName(f.Path).Equals(FileName(file.RelativePath), StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                unused.Remove(match);
            }

            pairs.Add((file, match?.Path));
        }

        return pairs;
    }

    /// <summary>Where DAUpdater.exe puts an add-in's file: as DAORework does, except override files go straight into <c>override</c>.</summary>
    private static string DaUpdaterPath(DeployedFile file, string owned) =>
        file.IsOverride && file.TargetPath.StartsWith(owned, StringComparison.OrdinalIgnoreCase)
            ? ModLayout.OverridePrefix + file.TargetPath[owned.Length..]
            : file.TargetPath;

    private static string? Present(string path, long size, Dictionary<string, long> sizes) =>
        sizes.TryGetValue(path, out var actual) && actual == size ? path : null;

    private static Dictionary<string, long> Sizes(HandInstalledSnapshot snapshot) =>
        snapshot.AddInFolders.Values.SelectMany(f => f)
            .Concat(snapshot.OverrideFolders.Values.SelectMany(f => f))
            .Concat(snapshot.LooseOverrideFiles)
            .GroupBy(f => f.Path, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().SizeBytes, StringComparer.OrdinalIgnoreCase);

    private static string FileName(string path) => path[(path.LastIndexOf('/') + 1)..];
}
