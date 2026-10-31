using DAORework.Core.Catalog;
using DAORework.Core.Common;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;

namespace DAORework.Core.Mods.Adoption;

/// <summary>
/// Sorts what is hand-installed in a documents folder into adoption candidates and what cannot be adopted (ADR-0015,
/// ROADMAP P2.5-02). Pure: the snapshot, the deployment record and the catalog come in; nothing is read or written.
/// What the record holds is already managed and is left out. Official DLC is never a candidate.
/// </summary>
public static class AdoptionScanner
{
    private const string AddInsPrefix = GamePaths.AddInsDirectoryName + "/";

    public static AdoptionScan Scan(HandInstalledSnapshot snapshot, DeploymentRecord record, KnownItemsCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(catalog);

        var known = new AdoptionCatalog(catalog);
        var candidates = new List<AdoptionCandidate>();
        var exclusions = new List<AdoptionExclusion>();
        SortAddIns(snapshot, ManagedAddIns(record), known, candidates, exclusions);
        candidates.AddRange(OverrideCandidates(snapshot, known));
        return new AdoptionScan(
            candidates.OrderBy(c => c.Id, StringComparer.OrdinalIgnoreCase).ToList(),
            exclusions.OrderBy(e => e.Id, StringComparer.OrdinalIgnoreCase).ToList());
    }

    /// <summary>The add-ins DAORework installed: the entries it added, and every AddIns folder it placed a file in.</summary>
    private static HashSet<string> ManagedAddIns(DeploymentRecord record)
    {
        var managed = record.AddedAddIns.Select(a => a.Uid).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var path in record.Files.Select(f => f.TargetPath).Where(p => p.StartsWith(AddInsPrefix, StringComparison.OrdinalIgnoreCase)))
        {
            var folder = path[AddInsPrefix.Length..].Split('/')[0];
            managed.Add(folder);
        }

        return managed;
    }

    private static void SortAddIns(
        HandInstalledSnapshot snapshot,
        HashSet<string> managed,
        AdoptionCatalog known,
        List<AdoptionCandidate> candidates,
        List<AdoptionExclusion> exclusions)
    {
        var folders = snapshot.AddInFolders.Keys.ToDictionary(k => k, k => k, StringComparer.OrdinalIgnoreCase);
        var registered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var addIn in snapshot.RegisteredAddIns.Where(a => !managed.Contains(a.Uid) && registered.Add(a.Uid)))
        {
            var id = AdoptionIds.AddIn(addIn.Uid);
            if (known.IsOfficial(addIn.Uid))
            {
                exclusions.Add(new AdoptionExclusion(id, addIn.Title, AdoptionExclusionReason.Official));
            }
            else if (!folders.TryGetValue(addIn.Uid, out var folder))
            {
                exclusions.Add(new AdoptionExclusion(id, addIn.Title, AdoptionExclusionReason.NoFolderInDocuments));
            }
            else
            {
                candidates.Add(new AdoptionCandidate(id, AdoptionCandidateKind.AddIn, addIn.Title, AddInsPrefix + folder, snapshot.AddInFolders[folder])
                {
                    CatalogItem = known.ForAddIn(addIn.Uid),
                    DeployedByVortex = snapshot.VortexDeployedAddIns,
                });
            }
        }

        foreach (var folder in folders.Keys.Where(f => !managed.Contains(f) && !registered.Contains(f)))
        {
            var reason = known.IsOfficial(folder) ? AdoptionExclusionReason.Official : AdoptionExclusionReason.NotRegistered;
            exclusions.Add(new AdoptionExclusion(AdoptionIds.AddIn(folder), folder, reason));
        }
    }

    private static List<AdoptionCandidate> OverrideCandidates(HandInstalledSnapshot snapshot, AdoptionCatalog known)
    {
        var candidates = snapshot.OverrideFolders
            .Where(f => f.Value.Count > 0 && !f.Key.Equals(ModLayout.OwnedOverrideFolder, StringComparison.OrdinalIgnoreCase))
            .Select(f => Override(AdoptionIds.OverrideFolder(f.Key), AdoptionCandidateKind.OverrideFolder, f.Key, ModLayout.OverridePrefix + f.Key, f.Value))
            .ToList();
        if (snapshot.LooseOverrideFiles.Count > 0)
        {
            candidates.Add(Override(
                AdoptionIds.LooseOverrideFiles,
                AdoptionCandidateKind.LooseOverrideFiles,
                "Loose files in override",
                ModLayout.OverridePrefix.TrimEnd('/'),
                snapshot.LooseOverrideFiles));
        }

        return candidates;

        AdoptionCandidate Override(string id, AdoptionCandidateKind kind, string name, string path, IReadOnlyList<HandInstalledFile> files) =>
            new(id, kind, name, path, files) { CatalogItem = known.ForOverrideFiles(files), DeployedByVortex = snapshot.VortexDeployedOverride };
    }
}
